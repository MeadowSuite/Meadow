using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Types;
using Meadow.JsonRpc.Types.Debugging;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.JsonRpc
{

    /// <summary>
    /// Interface specifying all standard Ethereum RPC methods, as well as some 
    /// testing/debugging methods commonly found in test node implementations. 
    /// And some unique methods implemented by this project. 
    /// </summary>
    public interface IRpcController : IRpcControllerMinimal
    {
        /// <summary>
        /// net_version - Returns the current network id.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#net_version"/>
        /// </summary>
        /// <returns>
        /// String - The current network id. Examples:
        /// "1": Ethereum Mainnet
        /// "2": Morden Testnet(deprecated)
        /// "3": Ropsten Testnet
        /// "4": Rinkeby Testnet
        /// "42": Kovan Testnet
        /// </returns>
        [RpcApiMethod(RpcApiMethod.net_version)]
        Task<string> Version();

        /// <summary>
        /// eth_protocolVersion - Returns the current ethereum protocol version.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_protocolversion"/>
        /// </summary>
        /// <returns>String - The current ethereum protocol version.</returns>
        [RpcApiMethod(RpcApiMethod.eth_protocolVersion)]
        Task<string> ProtocolVersion();

        /// <summary>
        /// evm_mine - 
        /// Special non-standard ganache client methods (not included within the original RPC specification).
        /// Force a block to be mined. Takes no parameters. Mines a block independent of whether or not mining is started or stopped.
        /// <see href="https://github.com/trufflesuite/ganache-cli#implemented-methods"/>
        /// </summary>
        [RpcApiMethod(RpcApiMethod.evm_mine)]
        Task Mine();

        /// <summary>
        /// evm_snapshot  - 
        /// Special non-standard ganache client methods (not included within the original RPC specification).
        /// Snapshot the state of the blockchain at the current block. Takes no parameters. Returns the integer id of the snapshot created.
        /// <see href="https://github.com/trufflesuite/ganache-cli#implemented-methods"/>
        /// </summary>
        [RpcApiMethod(RpcApiMethod.evm_snapshot)]
        Task<ulong> Snapshot();

        /// <summary>
        /// evm_revert  - 
        /// Special non-standard ganache client methods (not included within the original RPC specification).
        /// Revert the state of the blockchain to a previous snapshot. Takes a single parameter, which is the snapshot id to revert to. 
        /// If no snapshot id is passed it will revert to the latest snapshot. Returns true.
        /// <see href="https://github.com/trufflesuite/ganache-cli#implemented-methods"/>
        /// <param name="snapshotID">The snapshot id to revert to.</param>
        /// </summary>
        [RpcApiMethod(RpcApiMethod.evm_revert)]
        Task<bool> Revert(ulong snapshotID);

        /// <summary>
        /// evm_increaseTime  - 
        /// Special non-standard ganache client methods (not included within the original RPC specification).
        /// Jump forward in time. Takes one parameter, which is the amount of time to increase in seconds. Returns the total time adjustment, in seconds.
        /// <see href="https://github.com/trufflesuite/ganache-cli#implemented-methods"/>
        /// </summary>
        [RpcApiMethod(RpcApiMethod.evm_increaseTime)]
        Task IncreaseTime(ulong seconds);

        /// <summary>
        /// eth_getBalance - Returns the balance of the account of given address.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_getbalance"/>
        /// </summary>
        /// <param name="account">20 Bytes - address to check for balance</param>
        /// <param name="blockParameter">Integer block number, or the string "latest", "earliest" or "pending".</param>
        /// <returns>Eth balance in wei</returns>
        [RpcApiMethod(RpcApiMethod.eth_getBalance)]
        Task<UInt256> GetBalance(Address account, DefaultBlockParameter blockParameter);

        /// <summary>
        /// eth_blockNumber - Returns the number of most recent block.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_blocknumber"/>
        /// </summary>
        [RpcApiMethod(RpcApiMethod.eth_blockNumber)]
        Task<ulong> BlockNumber();

        /// <summary>
        /// eth_getBlockByHash - Returns information about a block by hash.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_getblockbyhash"/>
        /// </summary>
        /// <param name="hash">32 Bytes - Hash of a block.</param>
        /// <param name="getFullTransactionObjects">
        /// If true it returns the full transaction objects, if false only the hashes of the transactions.
        /// </param>
        /// <returns>A block object, or null when no block was found.</returns>
        [RpcApiMethod(RpcApiMethod.eth_getBlockByHash)]
        Task<Block> GetBlockByHash(Hash hash, bool getFullTransactionObjects);

        /// <summary>
        /// eth_getBlockByNumber - Returns information about a block by block number.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_getblockbynumber"/>
        /// </summary>
        /// <param name="getFullTransactionObjects">
        /// If true it returns the full transaction objects, if false only the hashes of the transactions.
        /// </param>
        /// <param name="blockParameter">Integer block number, or the string "latest", "earliest" or "pending".</param>
        [RpcApiMethod(RpcApiMethod.eth_getBlockByNumber)]
        Task<Block> GetBlockByNumber(DefaultBlockParameter blockParameter, bool getFullTransactionObjects);

        /// <summary>
        /// eth_sendRawTransaction - Creates new message call transaction or a contract creation for signed transactions.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_sendrawtransaction"/>
        /// </summary>
        /// <param name="signedData">The signed transaction data.</param>
        /// <returns>The transaction hash, or the zero hash if the transaction is not yet available.</returns>
        [RpcApiMethod(RpcApiMethod.eth_sendRawTransaction)]
        Task<Hash> SendRawTransaction(byte[] signedData);

        /// <summary>
        /// eth_getTransactionByHash - Returns the information about a transaction requested by transaction hash.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_gettransactionbyhash"/>
        /// </summary>
        /// <param name="transactionHash">32 Bytes - hash of a transaction.</param>
        /// <returns>A transaction object, or null when no transaction was found.</returns>
        [RpcApiMethod(RpcApiMethod.eth_getTransactionByHash)]
        Task<TransactionObject> GetTransactionByHash(Hash transactionHash);

        /// <summary>
        /// eth_getTransactionByBlockHashAndIndex - Returns information about a transaction by block hash and transaction index position.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_gettransactionbyblockhashandindex"/>
        /// </summary>
        /// <param name="blockHash">32 Bytes - hash of a block.</param>
        /// <param name="transactionIndex">Tnteger of the transaction index position.</param>
        /// <returns>A transaction object, or null when no transaction was found:</returns>
        [RpcApiMethod(RpcApiMethod.eth_getTransactionByBlockHashAndIndex)]
        Task<TransactionObject> GetTransactionByBlockHashAndIndex(Hash blockHash, ulong transactionIndex);

        /// <summary>
        /// eth_getBlockTransactionCountByHash - Returns the number of transactions in a block from a block matching the given block hash.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_getblocktransactioncountbyhash"/>
        /// </summary>
        /// <param name="blockHash">32 Bytes - hash of a block</param>
        /// <returns>Integer of the number of transactions in this block.</returns>
        [RpcApiMethod(RpcApiMethod.eth_getBlockTransactionCountByHash)]
        Task<ulong> GetBlockTransactionCountByHash(Hash blockHash);

        /// <summary>
        /// eth_getBlockTransactionCountByNumber - Returns the number of transactions in a block matching the given block number.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_getblocktransactioncountbynumber"/>
        /// </summary>
        /// <param name="blockParameter">Integer of a block number, or the string "earliest", "latest" or "pending", as in the default block parameter.</param>
        /// <returns>Integer of the number of transactions in this block.</returns>
        [RpcApiMethod(RpcApiMethod.eth_getBlockTransactionCountByNumber)]
        Task<ulong> GetBlockTransactionCountByNumber(DefaultBlockParameter blockParameter);

        /// <summary>
        /// eth_coinbase - Returns the client coinbase address.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_coinbase"/>
        /// </summary>
        /// <returns>20 bytes - the current coinbase address.</returns>
        [RpcApiMethod(RpcApiMethod.eth_coinbase)]
        Task<Address> Coinbase();

        /// <summary>
        /// eth_newBlockFilter - Creates a filter in the node, to notify when a new block arrives. To check if the state has changed, call eth_getFilterChanges.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_newblockfilter"/>
        /// </summary>
        /// <returns>A filter id.</returns>
        [RpcApiMethod(RpcApiMethod.eth_newBlockFilter)]
        Task<ulong> NewBlockFilter();

        /// <summary>
        /// eth_getFilterChanges - Polling method for a filter, which returns an array of logs which occurred since last poll.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_getfilterchanges"/>
        /// </summary>
        /// <param name="filterID">The filter id.</param>
        /// <returns>Array of log objects, or an empty array if nothing has changed since last poll.</returns>
        [RpcApiMethod(RpcApiMethod.eth_getFilterChanges)]
        Task<LogObjectResult> GetFilterChanges(ulong filterID);

        /// <summary>
        /// eth_getFilterLogs - Returns an array of all logs matching filter with given id.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_getfilterlogs"/>
        /// </summary>
        /// <param name="filterID">The filter id.</param>
        /// <returns>Array of log objects, or an empty array if nothing has changed since last poll.</returns>
        [RpcApiMethod(RpcApiMethod.eth_getFilterLogs)]
        Task<LogObjectResult> GetFilterLogs(ulong filterID);

        /// <summary>
        /// eth_getlogs - Returns an array of all logs matching a given filter object.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_getlogs"/>
        /// </summary>
        /// <param name="filterOptions">the filter object</param>
        /// <returns>Array of log objects, or an empty array if nothing has changed since last poll.</returns>
        [RpcApiMethod(RpcApiMethod.eth_getLogs)]
        Task<LogObjectResult> GetLogs(FilterOptions filterOptions);

        /// <summary>
        /// eth_newFilter - Creates a filter object, based on filter options, to notify when the state changes (logs). 
        /// To check if the state has changed, call eth_getFilterChanges.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_newfilter"/>
        /// </summary>
        /// <param name="filterOptions">the filter object</param>
        /// <returns>A filter id.</returns>
        [RpcApiMethod(RpcApiMethod.eth_newFilter)]
        Task<ulong> NewFilter(FilterOptions filterOptions);

        /// <summary>
        /// eth_uninstallFilter - Uninstalls a filter with given id. Should always be called when watch is no longer needed. 
        /// Additonally Filters timeout when they aren't requested with eth_getFilterChanges for a period of time.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_uninstallfilter"/>
        /// </summary>
        /// <param name="filterID">The filter id.</param>
        /// <returns>True if the filter was successfully uninstalled, otherwise false.</returns>
        [RpcApiMethod(RpcApiMethod.eth_uninstallFilter)]
        Task<bool> UninstallFilter(ulong filterID);

        /// <summary>
        /// eth_sign - The sign method calculates an Ethereum specific signature with: sign(keccak256("\x19Ethereum Signed Message:\n" + len(message) + message))).
        /// By adding a prefix to the message makes the calculated signature recognisable as an Ethereum specific signature.This prevents misuse where 
        /// a malicious DApp can sign arbitrary data (e.g.transaction) and use the signature to impersonate the victim.
        /// Note the address to sign with must be unlocked.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_sign"/>
        /// </summary>
        /// <param name="account">20 Bytes - address.</param>
        /// <param name="message">N Bytes - message to sign.</param>
        /// <returns>Signature.</returns>
        [RpcApiMethod(RpcApiMethod.eth_sign)]
        Task<byte[]> Sign(Address account, byte[] message);

        /// <summary>
        /// eth_syncing - Returns an object with data about the sync status or false.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_syncing"/>
        /// </summary>
        /// <returns>An object with sync status data or FALSE, when not syncing.</returns>
        [RpcApiMethod(RpcApiMethod.eth_syncing)]
        Task<SyncStatus> Syncing();

        /// <summary>
        /// eth_getCode - Returns code at a given address.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_getcode"/>
        /// </summary>
        /// <param name="address">20 Bytes - address.</param>
        /// <param name="blockParameter">Integer block number, or the string "latest", "earliest" or "pending", see the default block parameter</param>
        /// <returns>The code from the given address.</returns>
        [RpcApiMethod(RpcApiMethod.eth_getCode)]
        Task<byte[]> GetCode(Address address, DefaultBlockParameter blockParameter);

        /// <summary>
        /// eth_getCompilers - Returns a list of available compilers in the client.
        /// </summary>
        /// <returns>Array of available compilers.</returns>
        [RpcApiMethod(RpcApiMethod.eth_getCompilers)]
        Task<string[]> GetCompilers();

        /// <summary>
        /// web3_sha3 - Returns Keccak-256 (not the standardized SHA3-256) of the given data.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#web3_sha3"/>
        /// </summary>
        /// <param name="data">The data to convert into a SHA3 hash</param>
        /// <returns>The SHA3 result of the given string.</returns>
        [RpcApiMethod(RpcApiMethod.web3_sha3)]
        Task<Hash> Sha3(byte[] data);

        /// <summary>
        /// web3_clientVersion - Returns the current client version.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#web3_clientversion"/>
        /// </summary>
        /// <returns>String - The current client version.</returns>
        [RpcApiMethod(RpcApiMethod.web3_clientVersion)]
        Task<string> ClientVersion();


        /// <summary>
        /// net_peerCount - Returns number of peers currently connected to the client.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#net_peercount"/>
        /// </summary>
        /// <returns>Integer of the number of connected peers.</returns>
        [RpcApiMethod(RpcApiMethod.net_peerCount)]
        Task<ulong> PeerCount();

        /// <summary>
        /// net_listening - Returns true if client is actively listening for network connections.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#net_listening"/>
        /// </summary>
        /// <returns>True when listening, otherwise false.</returns>
        [RpcApiMethod(RpcApiMethod.net_listening)]
        Task<bool> Listening();

        /// <summary>
        /// eth_mining - Returns true if client is actively mining new blocks.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_mining"/>
        /// </summary>
        /// <returns>Returns true of the client is mining, otherwise false.</returns>
        [RpcApiMethod(RpcApiMethod.eth_mining)]
        Task<bool> Mining();

        /// <summary>
        /// eth_hashrate - Returns the number of hashes per second that the node is mining with.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_hashrate"/>
        /// </summary>
        /// <returns>Number of hashes per second.</returns>
        [RpcApiMethod(RpcApiMethod.eth_hashrate)]
        Task<UInt256> HashRate();

        /// <summary>
        /// eth_getTransactionCount - Returns the number of transactions sent from an address.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_gettransactioncount"/>
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="blockParameter">Integer block number, or the string "latest", "earliest" or "pending".</param>
        /// <returns>Integer of the number of transactions sent from this address.</returns>
        [RpcApiMethod(RpcApiMethod.eth_getTransactionCount)]
        Task<ulong> GetTransactionCount(Address address, DefaultBlockParameter blockParameter);

        /// <summary>
        /// Returns the currently configured chain id, a value used in replay-protected transaction signing as introduced by EIP-155.
        /// <see href="https://github.com/ethereum/EIPs/blob/master/EIPS/eip-695.md"/>
        /// </summary>
        /// <returns>Integer of the current chain id.</returns>
        [RpcApiMethod(RpcApiMethod.eth_chainId)]
        Task<ulong> ChainID();

        #region Custom RPC Methods

        /// <summary>
        /// Obtains a single coverage map which describes code coverage for the contract at the given address.
        /// </summary>
        /// <param name="contractAddress">The address which we wish to obtain coverage maps for.</param>
        /// <returns>Returns the instructionIndex->executionCount map and the jump index array for the contract at the specified address. If a coverage map does not exist, map will be null.</returns>
        [RpcApiMethod(RpcApiMethod.testing_getSingleCoverageMap)]
        Task<CompoundCoverageMap> GetCoverageMap(Address contractAddress);
        /// <summary>
        /// testing_clearSingleCoverage - Clears a coverage map for a contract at the specified contract address.
        /// </summary>
        /// <param name="contractAddress">The address of the contract for which we wish to remove a coverage map.</param>
        /// <returns>Returns true if a coverage map existed at the contract address, returns false if one did not.</returns>
        [RpcApiMethod(RpcApiMethod.testing_clearSingleCoverage)]
        Task<bool> ClearCoverage(Address contractAddress);

        /// <summary>
        /// testing_clearAllCoverage - Clears all coverage maps in our test chain configuration.
        /// </summary>
        [RpcApiMethod(RpcApiMethod.testing_clearAllCoverage)]
        Task ClearCoverage();

        /// <summary>
        /// testing_setCoverageEnabled - Sets the enabled/disabled status of coverage collection.
        /// </summary>
        [RpcApiMethod(RpcApiMethod.testing_setCoverageEnabled)]
        Task SetCoverageEnabled(bool enabled);

        /// <summary>
        /// Obtains all coverage maps which describe code coverage for all contracts ran since the last code coverage clear.
        /// </summary>
        /// <returns>Returns an array of tuples which specify contract address, code coverage map (instruction index -> execution count), and the instruction indexes from which we did jump after executing.</returns>
        [RpcApiMethod(RpcApiMethod.testing_getAllCoverageMaps)]
        Task<CompoundCoverageMap[]> GetAllCoverageMaps();


        /// <summary>
        /// testing_setTracingEnabled - Sets the enabled/disabled status of debug tracing.
        /// </summary>
        [RpcApiMethod(RpcApiMethod.testing_setTracingEnabled)]
        Task SetTracingEnabled(bool enabled);

        /// <summary>
        /// Obtains the last execution trace, assuming execution tracing was enabled and captured.
        /// </summary>
        /// <returns>Returns the last execution trace.</returns>
        [RpcApiMethod(RpcApiMethod.testing_getExecutionTrace)]
        Task<ExecutionTrace> GetExecutionTrace();

        /// <summary>
        /// Obtains the pre-image (original data) for a given hash, if recorded in the database.
        /// </summary>
        /// <returns>Returns the pre-image if one exists, null otherwise.</returns>
        [RpcApiMethod(RpcApiMethod.testing_getHashPreimage)]
        Task<byte[]> GetHashPreimage(byte[] hash);

        /// <summary>
        /// Enables or disables the size check on deployed contracts.
        /// </summary>
        [RpcApiMethod(RpcApiMethod.testing_setContractSizeCheckDisabled)]
        Task SetContractSizeCheckDisabled(bool enabled);

        #endregion
    }
}
