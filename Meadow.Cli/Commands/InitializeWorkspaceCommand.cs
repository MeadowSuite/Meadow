using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Meadow.Core.EthTypes;
using Meadow.Core.RlpEncoding;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Transactions;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;

namespace Meadow.Cli.Commands
{

    [Cmdlet(ApprovedVerbs.Initialize, "Workspace")]
    public class InitializeWorkspaceCommand : PSCmdlet
    {
        public const string WORKSPACE_CONSOLE = "console";
        public const string WORKSPACE_DEVELOPMENT = "development";
        public const string WORKSPACE_MANUAL = "manual";

        [Parameter(Mandatory = false, Position = 0)]
        [ValidateSet(WORKSPACE_CONSOLE, WORKSPACE_DEVELOPMENT, WORKSPACE_MANUAL)]
        public string Setup { get; set; } = WORKSPACE_MANUAL;

        bool CheckManualSetup()
        {
            if (!Config.CheckFileExists(SessionState.Path.CurrentLocation.Path))
            {
                WriteWarning($"Cannot initialize workspace without configuration. Run '{CmdLetExtensions.GetCmdletName<ConfigCommand>()}' then run '{CmdLetExtensions.GetCmdletName<InitializeWorkspaceCommand>()}'");
                return false;
            }

            var config = this.ReadConfig();
            if (config.UseLocalAccounts && (GlobalVariables.AccountKeys?.Length).GetValueOrDefault() == 0)
            {
                var localAcountsFile = LocalAccountsUtil.GetDefaultFilePath(SessionState.Path.CurrentLocation.Path);
                if (!File.Exists(localAcountsFile))
                {
                    WriteWarning($"Accounts must be first created to initialize workspace using local accounts. Run '{CmdLetExtensions.GetCmdletName<NewAccountsCommand>()}' then run '{CmdLetExtensions.GetCmdletName<InitializeWorkspaceCommand>()}'");
                    return false;
                }

                if (LocalAccountsUtil.IsEncrypted(localAcountsFile))
                {
                    WriteWarning($"Accounts are encrypted and must be loaded with a password before initialize workspace. Run '{CmdLetExtensions.GetCmdletName<LoadAccountsCommand>()}' then run '{CmdLetExtensions.GetCmdletName<InitializeWorkspaceCommand>()}'");
                    return false;
                }
            }

            return true;
        }

        protected override void EndProcessing()
        {
            // Configure Out-Default to automatically place set last command output into variable "$__"
            // See: https://tommymaynard.com/three-ways-to-set-psdefaultparametervalues-2017/
            var psDefaultParameterValues = SessionState.PSVariable.Get("PSDefaultParameterValues");
            var paramDict = (DefaultParameterDictionary)psDefaultParameterValues.Value;
            paramDict["Out-Default:OutVariable"] = "__";

            if (Setup.Equals(WORKSPACE_MANUAL, StringComparison.OrdinalIgnoreCase))
            {
                if (!CheckManualSetup())
                {
                    return;
                }
            }

            if (!Config.CheckFileExists(SessionState.Path.CurrentLocation.Path))
            {
                var configResult = this.Execute<ConfigCommand>((nameof(ConfigCommand.Workspace), Setup));
            }

            var config = this.ReadConfig();

            string rpcServerUri;

            if (Setup.Equals(WORKSPACE_DEVELOPMENT, StringComparison.OrdinalIgnoreCase))
            {
                var startTestServerResult = this.Execute<StartTestServerCommand>();
                rpcServerUri = GlobalVariables.TestNodeServer.RpcServer.ServerAddresses[0];
            }
            else
            {
                var networkHost = config.NetworkHost;
                if (!networkHost.StartsWith("http:", StringComparison.OrdinalIgnoreCase) && !networkHost.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
                {
                    networkHost = "http://" + networkHost;
                }

                if (!Uri.TryCreate(networkHost, UriKind.Absolute, out var hostUri))
                {
                    Host.UI.WriteErrorLine($"Invalid network host / URI specified: '{networkHost}'");
                    return;
                }

                var uriBuilder = new UriBuilder(hostUri);

                bool portSpecifiedInHost = config.NetworkHost.Contains(":" + uriBuilder.Port, StringComparison.Ordinal);

                if (config.NetworkPort == 0 && !portSpecifiedInHost)
                {
                    Host.UI.WriteWarningLine($"The RPC server port is not specified in '{nameof(Config.NetworkHost)}' or '{nameof(Config.NetworkPort)}' config. The default port {uriBuilder.Uri.Port} for {uriBuilder.Scheme} will be used.");
                }

                if (config.NetworkPort != 0)
                {
                    if (portSpecifiedInHost)
                    {
                        Host.UI.WriteWarningLine($"A network port is specified in both config options {nameof(Config.NetworkHost)}={uriBuilder.Port} and {nameof(Config.NetworkPort)}={config.NetworkPort}. Only {uriBuilder.Port} will be used.");
                    }
                    else
                    {
                        uriBuilder.Port = (int)config.NetworkPort;
                    }
                }

                rpcServerUri = $"{uriBuilder.Scheme}://{uriBuilder.Host}:{uriBuilder.Port}";
            }


            var jsonRpcClient = JsonRpcClient.Create(
                new Uri(rpcServerUri),
                defaultGasLimit: config.DefaultGasLimit,
                defaultGasPrice: config.DefaultGasPrice);

            // Perform a json rpc version check to ensure rpc server is reachable
            try
            {
                var networkVersion = jsonRpcClient.Version().GetResultSafe();
                Host.UI.WriteLine($"RPC client connected to server {rpcServerUri}. Network version: {networkVersion}");
            }
            catch (Exception ex)
            {
                Host.UI.WriteErrorLine(ex.ToString());
                Host.UI.WriteErrorLine($"RPC client could not connect to RPC server at {rpcServerUri}. Check your network configuration with '{CmdLetExtensions.GetCmdletName<ConfigCommand>()}'.");

                if (Setup.Equals(WORKSPACE_MANUAL, StringComparison.OrdinalIgnoreCase))
                {
                    if (GlobalVariables.TestNodeServer != null)
                    {
                        Host.UI.WriteErrorLine($"A test RPC server is running at {GlobalVariables.TestNodeServer.RpcServer.ServerAddresses.First()}.");
                    }
                    else
                    {
                        Host.UI.WriteErrorLine($"To start an test RPC server run '{CmdLetExtensions.GetCmdletName<StartTestServerCommand>()}'.");
                    }
                }

                return;
            }

            SessionState.SetRpcClient(jsonRpcClient);

            Address[] accounts;

            if (config.UseLocalAccounts)
            {
                uint chainID;

                if (config.ChainID != 0)
                {
                    chainID = config.ChainID;
                }
                else
                {
                    try
                    {
                        var verString = jsonRpcClient.Version().GetResultSafe();
                        chainID = uint.Parse(verString, CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        Host.UI.WriteErrorLine(ex.ToString());
                        Host.UI.WriteErrorLine($"Could not detect chainID from server. Set '{nameof(Config.ChainID)}' using  with '{CmdLetExtensions.GetCmdletName<ConfigCommand>()}'.");
                        Host.UI.WriteLine($"Use {nameof(Config.ChainID)} of 1 for mainnet. See https://github.com/ethereum/EIPs/blob/master/EIPS/eip-155.md#list-of-chain-ids for more information.");
                        return;
                    }
                }

                if ((GlobalVariables.AccountKeys?.Length).GetValueOrDefault() == 0)
                {
                    var localAcountsFile = LocalAccountsUtil.GetDefaultFilePath(SessionState.Path.CurrentLocation.Path);
                    if (File.Exists(localAcountsFile))
                    {
                        if (LocalAccountsUtil.IsEncrypted(localAcountsFile))
                        {
                            WriteWarning($"Local account files exists but is encrypted. The session must be started manually to load encryped accounts. New accounts will be generated in memory instead.");
                            var newAccountsResult = this.Execute<NewAccountsCommand>();
                        }
                        else
                        {
                            var loadAccountsResult = this.Execute<LoadAccountsCommand>();
                        }
                    }
                    else
                    {
                        var newAccountsResult = this.Execute<NewAccountsCommand>();
                    }            
                }

                accounts = GlobalVariables.AccountKeys.Select(a => a.Address).ToArray();
                GlobalVariables.ChainID = chainID;
                jsonRpcClient.RawTransactionSigner = CreateRawTransactionSigner();
                jsonRpcClient.TransactionReceiptPollInterval = TimeSpan.FromMilliseconds(500);
            }
            else
            {
                accounts = jsonRpcClient.Accounts().GetResultSafe();
            }

            SessionState.SetAccounts(accounts);

            string solSourceDir = Util.GetSolSourcePath(config, SessionState);
            if (!Directory.Exists(solSourceDir))
            {
                Directory.CreateDirectory(solSourceDir);
            }

            if (Directory.EnumerateFiles(solSourceDir, "*.sol", SearchOption.AllDirectories).Any())
            {
                var compileSolResult = this.Execute<CompileSolidityCommand>();
            }

            var watchSolResult = this.Execute<WatchSolidityCommand>();

            bool failed = false;

            if (failed)
            {
                Environment.Exit(-1);
            }
        }

        static RawTransactionSignerDelegate CreateRawTransactionSigner()
        {
            return new RawTransactionSignerDelegate(async (rpcClient, txParams) =>
            {
                var account = GlobalVariables.AccountKeys.First(a => a.Address == txParams.From.Value);
                var nonce = await rpcClient.GetTransactionCount(account.Address, BlockParameterType.Pending);
                var chainID = GlobalVariables.ChainID;

                byte[] signedTxBytes = TransactionUtil.SignRawTransaction(
                    account.Account,
                    nonce, 
                    (BigInteger?)txParams.GasPrice ?? 0,
                    (BigInteger?)txParams.Gas ?? 0, 
                    txParams.To,
                    (BigInteger?)txParams.Value ?? 0, 
                    txParams.Data, 
                    chainID);

                return signedTxBytes;
            });
        }
    }
}
