using System.Threading.Tasks;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Types;
using Meadow.JsonRpc.Types.Debugging;

namespace Meadow.JsonRpc.Server.Test
{

    class MockRpcController : IRpcController
    {
        public Task<Address[]> Accounts()
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> BlockNumber()
        {
            throw new System.NotImplementedException();
        }

        public Task<byte[]> Call(CallParams ethCallParam, DefaultBlockParameter blockParameter)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> ChainID()
        {
            throw new System.NotImplementedException();
        }

        public Task ClearCoverage()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> ClearCoverage(Address contractAddress)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> ClientVersion()
        {
            throw new System.NotImplementedException();
        }

        public Task<Address> Coinbase()
        {
            throw new System.NotImplementedException();
        }

        public Task<UInt256> EstimateGas(CallParams callParams, DefaultBlockParameter blockParameter)
        {
            throw new System.NotImplementedException();
        }

        public Task<UInt256> GasPrice()
        {
            throw new System.NotImplementedException();
        }

        public Task<(Address contractAddress, uint[] map, int[] jumps)[]> GetAllCoverageMaps()
        {
            throw new System.NotImplementedException();
        }

        public Task<UInt256> GetBalance(Address account, DefaultBlockParameter blockParameter)
        {
            throw new System.NotImplementedException();
        }

        public Task<Block> GetBlockByHash(Hash hash, bool getFullTransactionObjects)
        {
            throw new System.NotImplementedException();
        }

        public Task<Block> GetBlockByNumber(DefaultBlockParameter blockParameter, bool getFullTransactionObjects)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> GetBlockTransactionCountByHash(Hash blockHash)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> GetBlockTransactionCountByNumber(DefaultBlockParameter blockParameter)
        {
            throw new System.NotImplementedException();
        }

        public Task<byte[]> GetCode(Address address, DefaultBlockParameter blockParameter)
        {
            throw new System.NotImplementedException();
        }

        public Task<string[]> GetCompilers()
        {
            throw new System.NotImplementedException();
        }

        public Task<(uint[] map, int[] jumps)> GetCoverageMap(Address contractAddress)
        {
            throw new System.NotImplementedException();
        }

        public Task<ExecutionTrace> GetExecutionTrace()
        {
            throw new System.NotImplementedException();
        }

        public Task<byte[]> GetHashPreimage(byte[] hash)
        {
            throw new System.NotImplementedException();
        }


        public Task<LogObjectResult> GetFilterChanges(ulong filterID)
        {
            throw new System.NotImplementedException();
        }

        public Task<LogObjectResult> GetFilterLogs(ulong filterID)
        {
            throw new System.NotImplementedException();
        }

        public Task<LogObjectResult> GetLogs(FilterOptions filterOptions)
        {
            throw new System.NotImplementedException();
        }

        public Task<TransactionObject> GetTransactionByBlockHashAndIndex(Hash blockHash, ulong transactionIndex)
        {
            throw new System.NotImplementedException();
        }

        public Task<TransactionObject> GetTransactionByHash(Hash transactionHash)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> GetTransactionCount(Address address, DefaultBlockParameter blockParameter)
        {
            throw new System.NotImplementedException();
        }

        public Task<TransactionReceipt> GetTransactionReceipt(Hash transactionHash)
        {
            throw new System.NotImplementedException();
        }

        public Task<UInt256> HashRate()
        {
            throw new System.NotImplementedException();
        }

        public Task IncreaseTime(ulong seconds)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> Listening()
        {
            throw new System.NotImplementedException();
        }

        public Task Mine()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> Mining()
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> NewFilter(FilterOptions filterOptions)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> PeerCount()
        {
            throw new System.NotImplementedException();
        }

        public Task<string> ProtocolVersion()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> Revert(ulong snapshotID)
        {
            throw new System.NotImplementedException();
        }

        public Task<Hash> SendRawTransaction(byte[] signedData)
        {
            throw new System.NotImplementedException();
        }

        public Task<Hash> SendTransaction(TransactionParams transactionParam)
        {
            throw new System.NotImplementedException();
        }

        public Task SetContractSizeCheckDisabled(bool enabled)
        {
            throw new System.NotImplementedException();
        }

        public Task SetCoverageEnabled(bool enabled)
        {
            throw new System.NotImplementedException();
        }

        public Task SetTracingEnabled(bool enabled)
        {
            throw new System.NotImplementedException();
        }

        public Task<Hash> Sha3(byte[] data)
        {
            throw new System.NotImplementedException();
        }

        public Task<byte[]> Sign(Address account, byte[] message)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> Snapshot()
        {
            throw new System.NotImplementedException();
        }

        public Task<SyncStatus> Syncing()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> UninstallFilter(ulong filterID)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> Version()
        {
            throw new System.NotImplementedException();
        }

        Task<CompoundCoverageMap[]> IRpcController.GetAllCoverageMaps()
        {
            throw new System.NotImplementedException();
        }

        Task<CompoundCoverageMap> IRpcController.GetCoverageMap(Address contractAddress)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> NewBlockFilter()
        {
            throw new System.NotImplementedException();
        }
    }

}