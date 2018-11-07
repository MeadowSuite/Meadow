using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Server;
using Meadow.JsonRpc.Types;
using Meadow.JsonRpc.Types.Debugging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

namespace Meadow.JsonRpc.Server.Test
{
    public class MockServerApp : IRpcController
    {
        public JsonRpcHttpServer RpcServer;

        public MockServerApp(int? port = null)
        {
            RpcServer = new JsonRpcHttpServer(this, ConfigureWebHost, port);
        }

        IWebHostBuilder ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureLogging((hostingContext, logging) =>
            {
                //logging.AddFilter("System", LogLevel.Debug);
                //logging.AddFilter<DebugLoggerProvider>("Microsoft", LogLevel.Trace);
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddConsole();
                logging.AddDebug();
            });
        }

        Dictionary<Address, UInt256> _accounts = new Dictionary<Address, UInt256>
        {
            ["0x59B3d163AA7fA815ee78DEA5BD4E669334D74f8a"] = 123,
            ["0xA4BeF567688E3ae270b2eFCB5a8Bfe9eBD734738"] = 4444444,
            ["0x98E4625b2d7424C403B46366150AB28Df4063408"] = 8585885,
            ["0x40515114eEa1497D753659DFF85910F838c6B234"] = 0,
            ["0x0CC74938BDF2Ccec8c25BAAf91700C41600c39c1"] = UInt256.MaxValue
        };

        ulong _currentBlockNumber = 1;

        public Task<Address[]> Accounts()
        {
            return Task.FromResult(_accounts.Keys.ToArray());
        }

        public Task<string> Version()
        {
            return Task.FromResult("117");
        }

        public Task<ulong> ChainID()
        {
            return Task.FromResult((ulong)1);
        }

        public Task<string> ProtocolVersion()
        {
            return Task.FromResult("5555");
        }

        public Task Mine()
        {
            _currentBlockNumber++;
            return Task.CompletedTask;
        }

        public Task<UInt256> GetBalance(Address account, DefaultBlockParameter blockParameter)
        {
            var balance = _accounts[account];
            return Task.FromResult(balance);
        }

        public Task<ulong> BlockNumber()
        {
            return Task.FromResult(_currentBlockNumber);
        }

        public Task<ulong> Snapshot()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Revert(ulong snapshotID)
        {
            throw new NotImplementedException();
        }

        public Task IncreaseTime(ulong seconds)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> Call(CallParams callParams, DefaultBlockParameter blockParameter)
        {
            throw new NotImplementedException();
        }

        public Task<UInt256> GasPrice()
        {
            throw new NotImplementedException();
        }

        public Task<Block> GetBlockByHash(Hash hash, bool getFullTransactionObjects)
        {
            throw new NotImplementedException();
        }

        public Task<Block> GetBlockByNumber(DefaultBlockParameter blockParameter, bool getFullTransactionObjects)
        {
            throw new NotImplementedException();
        }

        public Task<Hash> SendRawTransaction(byte[] signedData)
        {
            throw new NotImplementedException();
        }

        public Task<Hash> SendTransaction(TransactionParams transactionParams)
        {
            throw new NotImplementedException();
        }

        public Task<TransactionReceipt> GetTransactionReceipt(Hash transactionHash)
        {
            throw new NotImplementedException();
        }

        public Task<TransactionObject> GetTransactionByHash(Hash transactionHash)
        {
            throw new NotImplementedException();
        }

        public Task<UInt256> EstimateGas(CallParams callParams, DefaultBlockParameter blockParameter)
        {
            throw new NotImplementedException();
        }

        public Task<TransactionObject> GetTransactionByBlockHashAndIndex(Hash blockHash, ulong transactionIndex)
        {
            throw new NotImplementedException();
        }

        public Task<ulong> GetBlockTransactionCountByHash(Hash blockHash)
        {
            throw new NotImplementedException();
        }

        public Task<ulong> GetBlockTransactionCountByNumber(DefaultBlockParameter blockParameter)
        {
            throw new NotImplementedException();
        }

        public Task<Address> Coinbase()
        {
            throw new NotImplementedException();
        }

        public Task<LogObjectResult> GetFilterChanges(ulong filterID)
        {
            throw new NotImplementedException();
        }

        public Task<LogObjectResult> GetFilterLogs(ulong filterID)
        {
            //return Task.FromResult<LogObjectResult>(null);
            var logObjectResult = new LogObjectResult();

            Data d1 = (Data)Enumerable.Range(1, 32).Select(t => (byte)t).ToArray();
            Data d2 = (Data)Enumerable.Range(100, 32).Select(t => (byte)t).ToArray();
            Data d3 = (Data)Enumerable.Range(2, 32).Select(t => (byte)t).ToArray();
            Data d4 = (Data)Enumerable.Range(200, 32).Select(t => (byte)t).ToArray();

            Hash h1 = (Hash)Enumerable.Range(134, 32).Select(t => (byte)t).ToArray();

            logObjectResult.ResultType = LogObjectResultType.LogObjects;
            logObjectResult.LogObjects = new FilterLogObject[]
            {
                new FilterLogObject
                {
                    Address = Address.Zero,
                    BlockNumber = 23444,
                    Topics = new[] { d1, d2 },
                    LogIndex = 989
                },
                new FilterLogObject
                {
                    TransactionIndex = 3,
                    Data = Enumerable.Range(200, 32).Select(t => (byte)t).ToArray(),
                    BlockHash = h1
                }
            };
            return Task.FromResult(logObjectResult);
        }

        public Task<LogObjectResult> GetLogs(FilterOptions filterOptions)
        {
            throw new NotImplementedException();
        }

        public Task<ulong> NewFilter(FilterOptions filterOptions)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UninstallFilter(ulong filterID)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> Sign(Address account, byte[] message)
        {
            throw new NotImplementedException();
        }

        public Task<SyncStatus> Syncing()
        {
            var result = new SyncStatus { IsSyncing = true, CurrentBlock = 999, HighestBlock = 2124, StartingBlock = 2 };
            return Task.FromResult(result);
        }

        public Task<byte[]> GetCode(Address address, DefaultBlockParameter blockParameter)
        {
            throw new NotImplementedException();
        }

        public Task<string[]> GetCompilers()
        {
            throw new NotImplementedException();
        }

        public Task<Hash> Sha3(byte[] data)
        {
            throw new NotImplementedException();
        }

        public Task<string> ClientVersion()
        {
            throw new NotImplementedException();
        }

        public Task<ulong> PeerCount()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Listening()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Mining()
        {
            throw new NotImplementedException();
        }

        public Task<UInt256> HashRate()
        {
            throw new NotImplementedException();
        }

        public Task<ulong> GetTransactionCount(Address address, DefaultBlockParameter blockParameter)
        {
            throw new NotImplementedException();
        }

        // Tracing
        public Task SetTracingEnabled(bool enabled)
        {
            throw new NotImplementedException();
        }

        // Coverage
        public Task SetCoverageEnabled(bool enabled)
        {
            throw new NotImplementedException();
        }

        public Task<CompoundCoverageMap> GetCoverageMap(Address contractAddress)
        {
            throw new NotImplementedException();
        }

        public Task<CompoundCoverageMap[]> GetAllCoverageMaps()
        {
            throw new NotImplementedException();
        }

        public Task ClearCoverage()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ClearCoverage(Address contractAddress)
        {
            throw new NotImplementedException();
        }

        public Task<ExecutionTrace> GetExecutionTrace()
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> GetHashPreimage(byte[] hash)
        {
            throw new NotImplementedException();
        }

        public Task SetContractSizeCheckDisabled(bool enabled)
        {
            throw new NotImplementedException();
        }

        public Task<ulong> NewBlockFilter()
        {
            throw new NotImplementedException();
        }
    }
}
