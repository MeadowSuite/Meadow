using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;
using Meadow.JsonRpc.Types.Debugging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Meadow.JsonRpc.Server.Proxy
{
    public class RpcServerProxy : IRpcController
    {
        readonly IRpcController _proxyClient;
        readonly JsonRpcHttpServer _httpServer;

        public IWebHost WebHost => _httpServer.WebHost;

        public RpcServerProxy(string host, int proxyServerPort, int targetServerPort)
        {
            _proxyClient = JsonRpcClient.Create(new Uri($"http://{host}:{targetServerPort}"), ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);
            _httpServer = new JsonRpcHttpServer(_proxyClient, ConfigureWebHost, proxyServerPort);

            var undefinedRpcMethods = this.GetUndefinedRpcMethods();
            Console.WriteLine("Warning: following RPC methods are not defined: \n" + string.Join(", ", undefinedRpcMethods.Select(r => r.Value())));
        }

        public void StartServer() => WebHost.Start();
        public Task StartServerAsync() => WebHost.StartAsync();
        public Task RunServerAsync() => WebHost.RunAsync();
        public Task WaitForServerShutdownAsync() => WebHost.WaitForShutdownAsync();

        IWebHostBuilder ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureLogging((hostingContext, logging) =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddConsole();
            });
        }

        public Task<Address[]> Accounts()
        {
            return _proxyClient.Accounts();
        }

        public Task<ulong> BlockNumber()
        {
            return _proxyClient.BlockNumber();
        }

        public Task<byte[]> Call(CallParams callParams, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.Call(callParams, blockParameter);
        }

        public Task<string> ClientVersion()
        {
            return _proxyClient.ClientVersion();
        }

        public Task<Address> Coinbase()
        {
            return _proxyClient.Coinbase();
        }

        public Task<UInt256> EstimateGas(CallParams callParams, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.EstimateGas(callParams, blockParameter);
        }

        public Task<UInt256> GasPrice()
        {
            return _proxyClient.GasPrice();
        }

        public Task<UInt256> GetBalance(Address account, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.GetBalance(account, blockParameter);
        }

        public Task<Block> GetBlockByHash(Hash hash, bool getFullTransactionObjects)
        {
            return _proxyClient.GetBlockByHash(hash, getFullTransactionObjects);
        }

        public Task<Block> GetBlockByNumber(bool getFullTransactionObjects, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.GetBlockByNumber(getFullTransactionObjects, blockParameter);
        }

        public Task<ulong> GetBlockTransactionCountByHash(Hash blockHash)
        {
            return _proxyClient.GetBlockTransactionCountByHash(blockHash);
        }

        public Task<ulong> GetBlockTransactionCountByNumber(DefaultBlockParameter blockParameter)
        {
            return _proxyClient.GetBlockTransactionCountByNumber(blockParameter);
        }

        public Task<byte[]> GetCode(Address address, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.GetCode(address, blockParameter);
        }

        public Task<string[]> GetCompilers()
        {
            return _proxyClient.GetCompilers();
        }

        public Task<LogObjectResult> GetFilterChanges(ulong filterID)
        {
            return _proxyClient.GetFilterChanges(filterID);
        }

        public Task<LogObjectResult> GetFilterLogs(ulong filterID)
        {
            return _proxyClient.GetFilterLogs(filterID);
        }

        public Task<LogObjectResult> GetLogs(FilterOptions filterOptions)
        {
            return _proxyClient.GetLogs(filterOptions);
        }

        public Task<TransactionObject> GetTransactionByBlockHashAndIndex(Hash blockHash, ulong transactionIndex)
        {
            return _proxyClient.GetTransactionByBlockHashAndIndex(blockHash, transactionIndex);
        }

        public Task<TransactionObject> GetTransactionByHash(Hash transactionHash)
        {
            return _proxyClient.GetTransactionByHash(transactionHash);
        }

        public Task<TransactionReceipt> GetTransactionReceipt(Hash transactionHash)
        {
            return _proxyClient.GetTransactionReceipt(transactionHash);
        }

        public Task IncreaseTime(ulong seconds)
        {
            return _proxyClient.IncreaseTime(seconds);
        }

        public Task Mine()
        {
            return _proxyClient.Mine();
        }

        public Task<ulong> NewFilter(FilterOptions filterOptions)
        {
            return _proxyClient.NewFilter(filterOptions);
        }

        public Task<string> ProtocolVersion()
        {
            return _proxyClient.ProtocolVersion();
        }

        public Task<bool> Revert(ulong snapshotID)
        {
            return _proxyClient.Revert(snapshotID);
        }

        public Task<Hash> SendRawTransaction(byte[] signedData)
        {
            return _proxyClient.SendRawTransaction(signedData);
        }

        public Task<Hash> SendTransaction(TransactionParams transactionParams)
        {
            return _proxyClient.SendTransaction(transactionParams);
        }

        public Task<Hash> Sha3(byte[] data)
        {
            return _proxyClient.Sha3(data);
        }

        public Task<byte[]> Sign(Address account, byte[] message)
        {
            return _proxyClient.Sign(account, message);
        }

        public Task<ulong> Snapshot()
        {
            return _proxyClient.Snapshot();
        }

        public Task<SyncStatus> Syncing()
        {
            return _proxyClient.Syncing();
        }

        public Task<bool> UninstallFilter(ulong filterID)
        {
            return _proxyClient.UninstallFilter(filterID);
        }

        public Task<string> Version()
        {
            return _proxyClient.Version();
        }

        public Task<ulong> PeerCount()
        {
            return _proxyClient.PeerCount();
        }

        public Task<bool> Listening()
        {
            return _proxyClient.Listening();
        }

        public Task<bool> Mining()
        {
            return _proxyClient.Mining();
        }

        public Task<UInt256> HashRate()
        {
            return _proxyClient.HashRate();
        }

        public Task<ulong> GetTransactionCount(Address address, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.GetTransactionCount(address, blockParameter);
        }

        // Tracing

        public Task SetTracingEnabled(bool enabled)
        {
            return _proxyClient.SetTracingEnabled(enabled);
        }

        // Coverage
        public Task SetCoverageEnabled(bool enabled)
        {
            return _proxyClient.SetCoverageEnabled(enabled);
        }

        public Task<CompoundCoverageMap> GetCoverageMap(Address contractAddress)
        {
            return _proxyClient.GetCoverageMap(contractAddress);
        }

        public Task<CompoundCoverageMap[]> GetAllCoverageMaps()
        {
            return _proxyClient.GetAllCoverageMaps();
        }

        public Task ClearCoverage()
        {
            return _proxyClient.ClearCoverage();
        }

        public Task<bool> ClearCoverage(Address contractAddress)
        {
            return _proxyClient.ClearCoverage(contractAddress);
        }

        public Task<ExecutionTrace> GetExecutionTrace()
        {
            return _proxyClient.GetExecutionTrace();
        }

        public Task SetContractSizeCheckDisabled(bool enabled)
        {
            return _proxyClient.SetContractSizeCheckDisabled(enabled);
        }

        public Task<ulong> ChainID()
        {
            return _proxyClient.ChainID();
        }
    }
}
