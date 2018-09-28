using ExposedObject;
using Meadow.Contract;
using Meadow.Core.AccountDerivation;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;
using Microsoft.PowerShell.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.Loader;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Meadow.Cli.Commands
{

    [Cmdlet(ApprovedVerbs.New, "Accounts")]
    [Alias("Generate-Accounts", "generateAccounts")]
    public class NewAccountsCommand : PSCmdlet
    {
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "12 word mnemonic phrase used for generating accounts.")]
        public string Mnemonic { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        public string HDPath { get; set; }

        protected override void EndProcessing()
        {
            var config = this.ReadConfig();

            IAccountDerivation accountDerivation;

            if (string.IsNullOrWhiteSpace(Mnemonic))
            {
                var hdWalletAccountDerivation = HDAccountDerivation.Create();
                accountDerivation = hdWalletAccountDerivation;
                Host.UI.WriteLine($"Using mnemonic phrase: '{hdWalletAccountDerivation.MnemonicPhrase}'");
                Host.UI.WriteErrorLine("Warning: this private key generation is not secure and should not be used in production.");
            }
            else
            {
                accountDerivation = new HDAccountDerivation(Mnemonic);
            }

            var accountKeys = new List<(Address Address, EthereumEcdsa Account)>();

            foreach (var account in EthereumEcdsa.Generate(config.AccountCount, accountDerivation))
            {
                // Get an account from the public key hash.
                var address = account.EcdsaKeyPairToAddress();
                accountKeys.Add((address, account));
            }

            var accounts = accountKeys.ToArray();
            GlobalVariables.AccountKeys = accounts;

            Console.WriteLine($"Created {accounts.Length} accounts. Use '{CmdLetExtensions.GetCmdletName<WriteAccountsCommand>()}' to save account keys to disk.");
        }

    }


    [Cmdlet(ApprovedVerbs.Read, "Accounts")]
    [Alias("readAccounts")]
    public class LoadAccountsCommand : PSCmdlet
    {
        
        [Parameter(Mandatory = false, Position = 0)]
        public string FilePath { get; set; } = LocalAccountsUtil.DEFAULT_FILE_NAME;

        //Changed string to secure string for use in console.  This should prevent the recall of the characters that comprise the password from Std error/Std out/Verbose messages.
        //I believe it does this by immediately sending the individual characters to a location in unmanaged memory and returning a pointer for retrieval.
        //This is also prioritized by GC when no longer needed, and called by the dispose method.
        //to use a secure string as the password -> Read-Accounts -FilePath 'Path/to/file' -Password (ConvertTo-SecureString -String 'password' -AsPlaintext -Force)
        //In both instances of use, I have had to convert the password and dispose of it immediately, so it does not stay in memory past the single command use and has to be reconverted for further uses.

        [Parameter(Mandatory = false, Position = 2)]
        public SecureString Password { get; set; }  

        protected override void EndProcessing()
        {
            string filePath;
            if (Path.IsPathRooted(FilePath))
            {
                filePath = Path.GetFullPath(FilePath);
            }
            else
            {
                filePath = Path.GetFullPath(Path.Join(SessionState.Path.CurrentLocation.Path, FilePath));
            }

            if (!File.Exists(filePath))
            {
                Host.UI.WriteErrorLine($"File does not exist at: {filePath}");
                return;
            }

            var fileContent = File.ReadAllText(filePath);
            var dataJson = JObject.Parse(fileContent);

            string[][] accountArrayHex;

            if (dataJson.TryGetValue(LocalAccountsUtil.JSON_ENCRYPTED_ACCOUNTS_KEY, out var token))
            {
                if (Password.Length == 0)
                {
                    Host.UI.WriteErrorLine($"No password parameter specified and accounts are encryped in file {FilePath}");
                    return;
                }
                else
                {
                    Password.MakeReadOnly();       
                }

                var encrypedAccounts = token.Value<string>();
                string decrypedContent;
                try
                {
                    decrypedContent = AesUtil.DecryptString(encrypedAccounts, Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(Password)));
                    Password.Dispose();
                }
                catch (Exception ex)
                {
                    Host.UI.WriteErrorLine(ex.ToString());
                    Host.UI.WriteErrorLine("Failed to decrypt account data. Incorrect password?");
                    return;
                }

                accountArrayHex = JsonConvert.DeserializeObject<string[][]>(decrypedContent);
            }
            else
            {
                if (Password != null && Password.Length > 0)
                {
                    Host.UI.WriteErrorLine($"Password parameter specified but accounts are encryped in file {FilePath}");
                    return;
                }

                accountArrayHex = dataJson[LocalAccountsUtil.JSON_ACCOUNTS_KEY].ToObject<string[][]>();
            }

            var accounts = accountArrayHex
                .Select(a => EthereumEcdsa.Create(HexUtil.HexToBytes(a[1]), EthereumEcdsaKeyType.Private))
                .Select(a => (a.EcdsaKeyPairToAddress(), a))
                .ToArray();

            GlobalVariables.AccountKeys = accounts;

            Host.UI.WriteLine($"Loaded {accounts.Length} accounts from {filePath}");
        }

    }


    [Cmdlet(ApprovedVerbs.Write, "Accounts")]
    [Alias("writeAccounts")]
    public class WriteAccountsCommand : PSCmdlet
    {

        [Parameter(Mandatory = false, Position = 0)]
        public string FilePath { get; set; } = LocalAccountsUtil.DEFAULT_FILE_NAME;

        [Parameter(Mandatory = false)]
        public SecureString Password { get; set; }

        [Parameter(Mandatory = false)]
        public bool EncryptData = true;

        protected override void EndProcessing()
        {
            if (GlobalVariables.AccountKeys == null || GlobalVariables.AccountKeys.Length == 0)
            {
                Host.UI.WriteErrorLine($"No accounts are loaded. Use '{CmdLetExtensions.GetCmdletName<NewAccountsCommand>()}' to generate accounts.");
                return;
            }

            string filePath;
            if (Path.IsPathRooted(FilePath))
            {
                filePath = Path.GetFullPath(FilePath);
            }
            else
            {
                filePath = Path.GetFullPath(Path.Join(SessionState.Path.CurrentLocation.Path, FilePath));
            }

            if (EncryptData && (Password == null || Password.Length == 0))
            {
                Host.UI.WriteErrorLine($"No '{nameof(Password)}' parameter is provided. To write without encryption set the '{nameof(EncryptData)}' parameter to false");
                return;
            }

            if ((Password != null && Password.Length > 0) && !EncryptData)
            {
                Host.UI.WriteErrorLine($"The '{nameof(EncryptData)}' parameter is set to false but the '{nameof(Password)}' parameter is provided. Pick one.");
                return;
            }

            var accounts = GlobalVariables.AccountKeys;

            var accountArrayHex = accounts
                .Select(a => new[] 
                {
                    a.Address.ToString(hexPrefix: true),
                    HexUtil.GetHexFromBytes(a.Account.ToPrivateKeyArray(), hexPrefix: true)
                })
                .ToArray();

            JObject dataObj = new JObject();

            if (EncryptData)
            {
                Password.MakeReadOnly();
                var accountJson = JsonConvert.SerializeObject(accountArrayHex, Formatting.Indented);
                var encrypedAccountsString = AesUtil.EncryptString(accountJson, Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(Password)));
                Password.Dispose();
                dataObj[LocalAccountsUtil.JSON_ENCRYPTED_ACCOUNTS_KEY] = encrypedAccountsString;
            }
            else
            {
                dataObj[LocalAccountsUtil.JSON_ACCOUNTS_KEY] = JArray.FromObject(accountArrayHex);
            }

            if (File.Exists(filePath))
            {
                var choices = new Collection<ChoiceDescription>(new[] 
                {
                    new ChoiceDescription("&Cancel"),
                    new ChoiceDescription("&Overwrite")
                });
                var overwrite = Host.UI.PromptForChoice($"File already exists at {filePath}", "Continue and overwite existing file?", choices, 0);
                if (overwrite != 1)
                {
                    Host.UI.WriteErrorLine("Accounts not saved to file.");
                    return;
                }
            }

            var dataJson = dataObj.ToString(Formatting.Indented);

            File.WriteAllText(filePath, dataJson);

            if (EncryptData)
            {
                Host.UI.WriteLine($"Wrote {accounts.Length} encrypted accounts to: {filePath}");
            }
            else
            {
                Host.UI.WriteLine($"Wrote {accounts.Length} unencrypted accounts to: {filePath}");
            }
        }
    }

    static class LocalAccountsUtil
    {
        public const string DEFAULT_FILE_NAME = "account_keys.json";

        public const string JSON_ACCOUNTS_KEY = "accounts";
        public const string JSON_ENCRYPTED_ACCOUNTS_KEY = "encrypted_accounts";

        public static string GetDefaultFilePath(string dir) => Path.GetFullPath(Path.Join(dir, DEFAULT_FILE_NAME));

        public static bool IsEncrypted(string filePath)
        {
            var fileContent = File.ReadAllText(filePath);
            var dataJson = JObject.Parse(fileContent);
            return dataJson.ContainsKey(JSON_ENCRYPTED_ACCOUNTS_KEY);
        }
    }
}
