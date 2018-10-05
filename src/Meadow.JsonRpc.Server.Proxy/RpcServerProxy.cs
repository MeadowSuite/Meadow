using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class RpcServerProxy : IRpcController, IDisposable
    {
        readonly IJsonRpcClient _proxyClient;
        readonly JsonRpcHttpServer _httpServer;

        public int ProxyServerPort => _httpServer.ServerPort;

        public IWebHost WebHost => _httpServer.WebHost;

        public RpcServerProxy(Uri targetHost, int? proxyServerPort = null, IPAddress address = null)
        {
            _proxyClient = JsonRpcClient.Create(targetHost, ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);
            _httpServer = new JsonRpcHttpServer(_proxyClient, ConfigureWebHost, proxyServerPort, address);
            
            //var undefinedRpcMethods = this.GetUndefinedRpcMethods();
            //Console.WriteLine("Warning: following RPC methods are not defined: \n" + string.Join(", ", undefinedRpcMethods.Select(r => r.Value())));
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

        public virtual Task<Address[]> Accounts()
        {
            return _proxyClient.Accounts();
        }

        public virtual Task<ulong> BlockNumber()
        {
            return _proxyClient.BlockNumber();
        }

        public virtual Task<byte[]> Call(CallParams callParams, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.Call(callParams, blockParameter);
        }

        public virtual Task<string> ClientVersion()
        {
            return _proxyClient.ClientVersion();
        }

        public virtual Task<Address> Coinbase()
        {
            return _proxyClient.Coinbase();
        }

        public virtual Task<UInt256> EstimateGas(CallParams callParams, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.EstimateGas(callParams, blockParameter);
        }

        public virtual Task<UInt256> GasPrice()
        {
            return _proxyClient.GasPrice();
        }

        public virtual Task<UInt256> GetBalance(Address account, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.GetBalance(account, blockParameter);
        }

        public virtual Task<Block> GetBlockByHash(Hash hash, bool getFullTransactionObjects)
        {
            return _proxyClient.GetBlockByHash(hash, getFullTransactionObjects);
        }

        public virtual Task<Block> GetBlockByNumber(bool getFullTransactionObjects, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.GetBlockByNumber(getFullTransactionObjects, blockParameter);
        }

        public virtual Task<ulong> GetBlockTransactionCountByHash(Hash blockHash)
        {
            return _proxyClient.GetBlockTransactionCountByHash(blockHash);
        }

        public virtual Task<ulong> GetBlockTransactionCountByNumber(DefaultBlockParameter blockParameter)
        {
            return _proxyClient.GetBlockTransactionCountByNumber(blockParameter);
        }

        public virtual Task<byte[]> GetCode(Address address, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.GetCode(address, blockParameter);
        }

        public virtual Task<string[]> GetCompilers()
        {
            return _proxyClient.GetCompilers();
        }

        public virtual Task<LogObjectResult> GetFilterChanges(ulong filterID)
        {
            return _proxyClient.GetFilterChanges(filterID);
        }

        public virtual Task<LogObjectResult> GetFilterLogs(ulong filterID)
        {
            return _proxyClient.GetFilterLogs(filterID);
        }

        public virtual Task<LogObjectResult> GetLogs(FilterOptions filterOptions)
        {
            return _proxyClient.GetLogs(filterOptions);
        }

        public virtual Task<TransactionObject> GetTransactionByBlockHashAndIndex(Hash blockHash, ulong transactionIndex)
        {
            return _proxyClient.GetTransactionByBlockHashAndIndex(blockHash, transactionIndex);
        }

        public virtual Task<TransactionObject> GetTransactionByHash(Hash transactionHash)
        {
            return _proxyClient.GetTransactionByHash(transactionHash);
        }

        public virtual Task<TransactionReceipt> GetTransactionReceipt(Hash transactionHash)
        {
            return _proxyClient.GetTransactionReceipt(transactionHash);
        }

        public virtual Task IncreaseTime(ulong seconds)
        {
            return _proxyClient.IncreaseTime(seconds);
        }

        public virtual Task Mine()
        {
            return _proxyClient.Mine();
        }

        public virtual Task<ulong> NewFilter(FilterOptions filterOptions)
        {
            return _proxyClient.NewFilter(filterOptions);
        }

        public virtual Task<string> ProtocolVersion()
        {
            return _proxyClient.ProtocolVersion();
        }

        public virtual Task<bool> Revert(ulong snapshotID)
        {
            return _proxyClient.Revert(snapshotID);
        }

        public virtual Task<Hash> SendRawTransaction(byte[] signedData)
        {
            return _proxyClient.SendRawTransaction(signedData);
        }

        public virtual Task<Hash> SendTransaction(TransactionParams transactionParams)
        {
            return _proxyClient.SendTransaction(transactionParams);
        }

        public virtual Task<Hash> Sha3(byte[] data)
        {
            return _proxyClient.Sha3(data);
        }

        public virtual Task<byte[]> Sign(Address account, byte[] message)
        {
            return _proxyClient.Sign(account, message);
        }

        public virtual Task<ulong> Snapshot()
        {
            return _proxyClient.Snapshot();
        }

        public virtual Task<SyncStatus> Syncing()
        {
            return _proxyClient.Syncing();
        }

        public virtual Task<bool> UninstallFilter(ulong filterID)
        {
            return _proxyClient.UninstallFilter(filterID);
        }

        public virtual Task<string> Version()
        {
            return _proxyClient.Version();
        }

        public virtual Task<ulong> PeerCount()
        {
            return _proxyClient.PeerCount();
        }

        public virtual Task<bool> Listening()
        {
            return _proxyClient.Listening();
        }

        public virtual Task<bool> Mining()
        {
            return _proxyClient.Mining();
        }

        public virtual Task<UInt256> HashRate()
        {
            return _proxyClient.HashRate();
        }

        public virtual Task<ulong> GetTransactionCount(Address address, DefaultBlockParameter blockParameter)
        {
            return _proxyClient.GetTransactionCount(address, blockParameter);
        }

        // Tracing

        public virtual Task SetTracingEnabled(bool enabled)
        {
            return _proxyClient.SetTracingEnabled(enabled);
        }

        // Coverage
        public virtual Task SetCoverageEnabled(bool enabled)
        {
            return _proxyClient.SetCoverageEnabled(enabled);
        }

        public virtual Task<CompoundCoverageMap> GetCoverageMap(Address contractAddress)
        {
            return _proxyClient.GetCoverageMap(contractAddress);
        }

        public virtual Task<CompoundCoverageMap[]> GetAllCoverageMaps()
        {
            return _proxyClient.GetAllCoverageMaps();
        }

        public virtual Task ClearCoverage()
        {
            return _proxyClient.ClearCoverage();
        }

        public virtual Task<bool> ClearCoverage(Address contractAddress)
        {
            return _proxyClient.ClearCoverage(contractAddress);
        }

        public virtual Task<ExecutionTrace> GetExecutionTrace()
        {
            return _proxyClient.GetExecutionTrace();
        }

        public virtual Task<byte[]> GetHashPreimage(byte[] hash)
        {
            return _proxyClient.GetHashPreimage(hash);
        }

        public virtual Task SetContractSizeCheckDisabled(bool enabled)
        {
            return _proxyClient.SetContractSizeCheckDisabled(enabled);
        }

        public virtual Task<ulong> ChainID()
        {
            return _proxyClient.ChainID();
        }

        public void Dispose()
        {
            _proxyClient.Dispose();
            _httpServer.Dispose();
        }
    }
}
