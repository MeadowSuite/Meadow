using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Server;
using Meadow.JsonRpc.Types;
using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types;
using Meadow.EVM.Data_Types.State;
using Meadow.EVM.Data_Types.Transactions;
using Meadow.EVM.EVM.Definitions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Meadow.EVM;
using Meadow.Core.Utils;
using Meadow.JsonRpc.Types.Debugging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Versioning;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Cryptography;
using Meadow.Core.RlpEncoding;

namespace Meadow.TestNode
{


    public class TestNodeServer : IRpcController, IDisposable
    {
        #region Fields
        private ulong _currentSnapshotId;
        private ulong _currentLogFilterId;
        #endregion

        #region Properties
        public JsonRpcHttpServer RpcServer;
        public TestNodeChain TestChain { get; }
        public List<Address> AccountKeys { get; private set; }
        public Dictionary<Address, EthereumEcdsa> AccountDictionary { get; private set; }
        public Dictionary<ulong, (StateSnapshot snapshot, TimeSpan timeStampOffset)> Snapshots { get; private set; }
        public Dictionary<ulong, FilterOptions> LogFilters { get; private set; }
        #endregion

        static TestNodeServer()
        {
            EthereumEcdsa.IncludeKeyDataInExceptions = true;
        }

        #region Constructor
        /// <param name="port">If null or unspecified the http server binds to a random port.</param>
        /// <param name="accountConfig">Configure number of accounts to generate, ether balance, wallet derivation method.</param>
        public TestNodeServer(int? port = null, AccountConfiguration accountConfig = null)
        {
            // Initialize our basic components.
            RpcServer = new JsonRpcHttpServer(this, ConfigureWebHost, port);
            AccountKeys = new List<Address>();
            AccountDictionary = new Dictionary<Address, EthereumEcdsa>();
            Snapshots = new Dictionary<ulong, (StateSnapshot snapshot, TimeSpan timeStampOffset)>();
            LogFilters = new Dictionary<ulong, FilterOptions>();

            // We create our genesis state by giving all of our genesis accounts a balance
            State genesisState = new State();

            // Set up a few accounts and give them balances in our genesis state.
            accountConfig = (accountConfig ?? new AccountConfiguration());
            BigInteger initialAccountBalance = new BigInteger(1e18M * accountConfig.DefaultAccountEtherBalance);

            // Generate a keypairs
            foreach (var keypair in EthereumEcdsa.Generate(accountConfig.AccountGenerationCount, accountConfig.AccountDerivationMethod))
            {
                // Get an account from the public key hash.
                Meadow.EVM.Data_Types.Addressing.Address account = new Meadow.EVM.Data_Types.Addressing.Address(keypair.GetPublicKeyHash());
                Address frontEndAddress = new Address(account.ToByteArray());

                // Set it in our lookup.
                AccountKeys.Add(frontEndAddress);
                AccountDictionary.Add(frontEndAddress, keypair);

                // Set our account's balance in our genesis state
                genesisState.SetBalance(account, initialAccountBalance);
            }

            // Commit our changes to the genesis state.
            genesisState.CommitChanges();

            // Create a configuration where we force the newest implemented version number. (And we provide our genesis state, and it's database so we can resolve items from it).
            Configuration configuration = new Configuration(genesisState.Configuration.Database, EthereumRelease.Byzantium, null, genesisState);

            // Set our chain ID
            configuration.ChainID = (EthereumChainID)77;

            // Set our genesis block difficulty (this is a special override case, if difficulty is 1, new difficulties will all be 1).
            configuration.GenesisBlock.Header.Difficulty = 1;

            // We disable ethash validation so we can process blocks quickly.
            configuration.IgnoreEthashVerification = true;

            // Set up our test chain
            TestChain = new TestNodeChain(configuration);
        }
        #endregion

        #region Functions
        IWebHostBuilder ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureLogging((hostingContext, logging) =>
            {
                //logging.AddFilter("System", LogLevel.Debug);
                //logging.AddFilter<DebugLoggerProvider>("Microsoft", LogLevel.Trace);
                logging.SetMinimumLevel(LogLevel.Debug);
            });
        }

        // Versioning
        public Task<string> Version()
        {
            // Obtain our version as an integer.
            // int version = (int)TestChain.Chain.Configuration.Version;

            // TODO: investigate this..
            // Other node implementations return chainID for version for clients to query and use for signing raw transactions,
            // since eth_chainId is not implemented or enabled on all other implementations for some reason.
            int version = (int)TestChain.Chain.Configuration.ChainID;

            return Task.FromResult(version.ToString(CultureInfo.InvariantCulture));
        }

        // Accounts
        public Task<Address[]> Accounts()
        {
            // Obtain our controlled accounts.
            return Task.FromResult(AccountKeys.ToArray());
        }

        public Task<UInt256> GetBalance(Address account, DefaultBlockParameter blockParameter)
        {
            // Obtain our account address
            Meadow.EVM.Data_Types.Addressing.Address accountAddress = new Meadow.EVM.Data_Types.Addressing.Address(account.GetBytes());

            // Obtain our balance from our state.
            BigInteger accountBalance = TestChain.Chain.State.GetBalance(accountAddress);

            // Return the balance.
            return Task.FromResult(new UInt256(accountBalance));
        }

        public Task<byte[]> GetCode(Address address, DefaultBlockParameter blockParameter)
        {
            // We want the state from this block parameter.
            State postBlockState = GetStateFromBlockParameters(blockParameter);
            if (postBlockState == null)
            {
                return Task.FromResult((byte[])null);
            }

            // Obtain our code for the account
            return Task.FromResult(postBlockState.GetCodeSegment(new Meadow.EVM.Data_Types.Addressing.Address(address.GetBytes())));
        }

        // Chain
        public Task IncreaseTime(ulong seconds)
        {
            // We offset our time by the given amount of seconds.
            TestChain.Chain.Configuration.CurrentTimestampOffset += TimeSpan.FromSeconds(seconds);

            return Task.CompletedTask;
        }

        public Task<UInt256> GasPrice()
        {
            // Return our minimum gas price.
            return Task.FromResult(new UInt256(TestChain.MinimumGasPrice));
        }

        public Task<Address> Coinbase()
        {
            // Return our address
            return Task.FromResult(new Address(TestChain.Coinbase.ToByteArray()));
        }

        public Task<ulong> ChainID()
        {
            return Task.FromResult((ulong)(int)TestChain.Chain.Configuration.ChainID);
        }

        public Task Mine()
        {
            // We force the mining of a block.
            TestChain.MiningUpdate(true);

            return Task.CompletedTask;
        }

        // Block Information
        public Task<ulong> BlockNumber()
        {
            // Obtain our most recent block number for our current state.
            return Task.FromResult((ulong)TestChain.Chain.State.CurrentBlock.Header.BlockNumber);
        }

        public Task<Block> GetBlockByHash(Hash hash, bool getFullTransactionObjects)
        {
            // Obtain the block by hash
            Meadow.EVM.Data_Types.Block.Block block = TestChain.Chain.GetBlock(hash.GetBytes());
            return Task.FromResult(JsonTypeConverter.CoreBlockToJsonBlock(TestChain, block));
        }

        public Task<Block> GetBlockByNumber(bool getFullTransactionObjects, DefaultBlockParameter blockParameter)
        {
            // Obtain the block by hash
            ulong blockNumber = (ulong)BlockNumberFromBlockParameters(blockParameter);

            // Obtain the block hash for this block number.
            byte[] blockHash = TestChain.Chain.GetBlockHashFromBlockNumber(blockNumber);

            // Obtain the block using the hash.
            Meadow.EVM.Data_Types.Block.Block block = TestChain.Chain.GetBlock(blockHash);

            // Return the block
            return Task.FromResult(JsonTypeConverter.CoreBlockToJsonBlock(TestChain, block));
        }

        // Transaction Information
        public Task<TransactionObject> GetTransactionByHash(Hash transactionHash)
        {
            // Obtain our transaction from a hash
            var result = TestChain.Chain.GetTransactionPosition(transactionHash.GetBytes());

            // TODO: If result is null, meaning the transaction hash is not known to the chain.

            // Obtain the block for this transaction
            byte[] blockHash = TestChain.Chain.GetBlockHashFromBlockNumber(result.Value.blockNumber);
            Meadow.EVM.Data_Types.Block.Block block = TestChain.Chain.GetBlock(blockHash);


            // Return our transaction.
            return Task.FromResult(JsonTypeConverter.CoreTransactionToJsonTransaction(block, (ulong)result.Value.transactionIndex));
        }

        public Task<TransactionObject> GetTransactionByBlockHashAndIndex(Hash blockHash, ulong transactionIndex)
        {
            // Obtain the block for this block hash.
            Meadow.EVM.Data_Types.Block.Block block = TestChain.Chain.GetBlock(blockHash.GetBytes());

            // TODO: If block is null

            // TODO: If transaction index is out of bounds.

            // Return our transaction.
            return Task.FromResult(JsonTypeConverter.CoreTransactionToJsonTransaction(block, transactionIndex));
        }

        public Task<ulong> GetBlockTransactionCountByHash(Hash blockHash)
        {
            // Obtain the block for this block hash.
            Meadow.EVM.Data_Types.Block.Block block = TestChain.Chain.GetBlock(blockHash.GetBytes());

            // If the block is null we'll say it has zero transactions.
            if (block == null)
            {
                return Task.FromResult((ulong)0);
            }

            // Return our transaction count.
            return Task.FromResult((ulong)block.Transactions.Length);
        }

        public Task<ulong> GetBlockTransactionCountByNumber(DefaultBlockParameter blockParameter)
        {
            // Obtain the block
            BigInteger blockNumber = BlockNumberFromBlockParameters(blockParameter);

            // Obtain our block from block number.
            byte[] blockHash = TestChain.Chain.GetBlockHashFromBlockNumber(blockNumber);
            Meadow.EVM.Data_Types.Block.Block block = TestChain.Chain.GetBlock(blockHash);

            // Return our transaction count.
            return Task.FromResult((ulong)block.Transactions.Length);
        }

        public Task<ulong> GetTransactionCount(Address address, DefaultBlockParameter blockParameter)
        {
            // Obtain the state for the block parameter
            State state = GetStateFromBlockParameters(blockParameter);
            if (state == null)
            {
                return Task.FromResult((ulong)0);
            }

            // Obtain the account's nonce for that address from that state. (The nonce is incremented for every transaction).
            BigInteger nonce = state.GetNonce(new Meadow.EVM.Data_Types.Addressing.Address(address.GetBytes()));
            return Task.FromResult((ulong)(nonce - TestChain.Chain.Configuration.InitialAccountNonce));
        }

        // Calls/Transactions
        public Task<Hash> SendRawTransaction(byte[] signedData)
        {
            // We assume the signed data is RLP encoded.
            RLPItem rlpTransaction = RLP.Decode(signedData);
            Meadow.EVM.Data_Types.Transactions.Transaction transaction = new Meadow.EVM.Data_Types.Transactions.Transaction(rlpTransaction);

            // Obtain our address we can presume the contract will be at once deployed (or if its already deployed)
            var sender = transaction.GetSenderAddress();
            var targetInformation = GetTransactionTargetInformation(sender, transaction.To);

            // Process the transaction and return the result
            return ProcessTransactionInternal(transaction, targetInformation.deploying, targetInformation.targetDeployedAddress);
        }

        public Task<UInt256> EstimateGas(CallParams callParams, DefaultBlockParameter blockParameter)
        {
            // Verify our block parameters are for the latest block.
            if (blockParameter.ParameterType == BlockParameterType.Earliest ||
                blockParameter.ParameterType == BlockParameterType.Pending)
            {
                return Task.FromException<UInt256>(new NotImplementedException("EstimateGas does not support estimations at earlier or pending points in the chain."));
            }

            if (blockParameter.ParameterType == BlockParameterType.BlockNumber && TestChain.Chain.State.CurrentBlock.Header.BlockNumber != blockParameter.BlockNumber)
            {
                return Task.FromException<UInt256>(new NotImplementedException("EstimateGas only supports estimations at the current block number in the chain."));
            }

            // Snapshot our state at this height in block
            ulong snapshotID = Snapshot().Result;

            // Process the transaction and obtain the transaction hash.
            var transactionHash = SendTransaction(new TransactionParams()
            {
                From = callParams.From,
                To = callParams.To,
                Data = callParams.Data,
                Gas = callParams.Gas,
                GasPrice = callParams.GasPrice,
                Nonce = null,
                Value = callParams.Value
            }).Result;

            // Obtain our transaction receipt.
            var transactionReceipt = GetTransactionReceipt(transactionHash).Result;

            // Revert to our previous state
            Revert(snapshotID);

            // Remove the snapshot from our lookup
            Snapshots.Remove(snapshotID);

            // Return our gas used.
            return Task.FromResult((UInt256)transactionReceipt.GasUsed);
        }

        public Task<Hash> SendTransaction(TransactionParams transactionParams)
        {
            // Create our default parameters
            Meadow.EVM.Data_Types.Addressing.Address to = new Meadow.EVM.Data_Types.Addressing.Address(0);
            if (transactionParams.To.HasValue)
            {
                to = new Meadow.EVM.Data_Types.Addressing.Address(transactionParams.To.Value.GetBytes());
            }

            // Obtain some information which helps us build our contract and determine deployment address if deploying.
            var sender = new Meadow.EVM.Data_Types.Addressing.Address(transactionParams.From.Value.GetBytes());
            var targetInformation = GetTransactionTargetInformation(sender, to);

            // Create our transaction using the provided parameters and some fallback options.
            Meadow.EVM.Data_Types.Transactions.Transaction transaction = new Meadow.EVM.Data_Types.Transactions.Transaction(
                transactionParams.Nonce ?? targetInformation.senderNonce,
                (BigInteger?)transactionParams.GasPrice ?? TestChain.MinimumGasPrice,
                (BigInteger?)transactionParams.Gas ?? GasDefinitions.CalculateGasLimit(TestChain.Chain.GetHeadBlock().Header, TestChain.Chain.Configuration),
                to,
                (BigInteger?)transactionParams.Value ?? 0,
                transactionParams.Data ?? Array.Empty<byte>());

            // Obtain the provided accounts keypair
            EthereumEcdsa keypair = AccountDictionary[transactionParams.From.Value];

            // If we're past the spurious dragon fork, we begin using chain ID.
            EthereumChainID? chainID = null;
            if (TestChain.Chain.Configuration.Version >= EthereumRelease.SpuriousDragon)
            {
                chainID = TestChain.Chain.Configuration.ChainID;
            }

            // Sign the transaction with this account's keypair
            transaction.Sign(keypair, chainID);

            // Process the transaction and return the result
            return ProcessTransactionInternal(transaction, targetInformation.deploying, targetInformation.targetDeployedAddress);
        }

        public Task<byte[]> Call(CallParams callParams, DefaultBlockParameter blockParameter)
        {
            // Obtain a state from this block parameter.
            State postBlockState = GetStateFromBlockParameters(blockParameter);
            if (postBlockState == null)
            {
                return Task.FromResult((byte[])null);
            }

            // Execute and obtain our result
            Meadow.EVM.EVM.Execution.EVMExecutionResult result = HandleCall(callParams, postBlockState);

            // TODO: After a typical/expected revert, end up with an unthrown Exception object
            Exception ex = TestChain.Chain.Configuration.DebugConfiguration.Error;
            if (ex != null)
            {
                return Task.FromException<byte[]>(ex);
            }

            // Return the return data
            return Task.FromResult(result.ReturnData.ToArray());
        }

        public Task<TransactionReceipt> GetTransactionReceipt(Hash transactionHash)
        {
            // Create a receipt instance
            TransactionReceipt receipt = new TransactionReceipt();

            // Obtain our block and transaction index from our hash
            var result = TestChain.Chain.GetTransactionPosition(transactionHash.GetBytes());

            if (!result.HasValue)
            {
                return Task.FromResult<TransactionReceipt>(null);
            }

            // Obtain the block for this transaction
            byte[] blockHash = TestChain.Chain.GetBlockHashFromBlockNumber(result.Value.blockNumber);
            Meadow.EVM.Data_Types.Block.Block block = TestChain.Chain.GetBlock(blockHash);

            // Obtain our transaction and receipt.
            int transactionIndex = (int)result.Value.transactionIndex;
            Meadow.EVM.Data_Types.Transactions.Transaction transaction = block.Transactions[(int)result.Value.transactionIndex];
            Meadow.EVM.Data_Types.Transactions.Receipt transactionReceipt = State.GetReceipt(block.Header, transactionIndex, TestChain.Chain.Configuration.Database);

            // Obtain our contract address (if we send this transaction to the create contract address)
            Meadow.EVM.Data_Types.Addressing.Address contractAddress = GetDeployedAddress(transaction.To, transaction.GetSenderAddress(), blockHash);

            // Create our filter log array
            FilterLogObject[] jsonLogs = new FilterLogObject[transactionReceipt.Logs.Count];
            for (int i = 0; i < jsonLogs.Length; i++)
            {
                // Grab the current log
                Meadow.EVM.Data_Types.Transactions.Log log = transactionReceipt.Logs[i];

                // Set it in our array
                jsonLogs[i] = JsonTypeConverter.CoreLogToJsonLog(log, i, (ulong)result.Value.blockNumber, (ulong)result.Value.transactionIndex, transactionHash, new Hash(blockHash));
            }

            // Determine if we succeeded. After byzantium, a single byte "1" is success, a blank array is fail. Otherwise it is just the actual state root hash.
            bool transactionSucceeded = ((transactionReceipt.StateRoot.Length == 1 && transactionReceipt.StateRoot[0] == 1) || transactionReceipt.StateRoot.Length == KeccakHash.HASH_SIZE);

            // Convert our receipt to our json format
            TransactionReceipt jsonReceipt = new TransactionReceipt()
            {
                BlockHash = new Hash(block.Header.GetHash()),
                BlockNumber = (ulong)result.Value.blockNumber,
                TransactionIndex = (ulong)result.Value.transactionIndex,
                TransactionHash = new Hash(transaction.GetHash()),
                CumulativeGasUsed = (ulong)transactionReceipt.GasUsed,
                LogsBloom = BigIntegerConverter.GetBytes(transactionReceipt.Bloom, EVMDefinitions.BLOOM_FILTER_SIZE),
                GasUsed = (ulong)transactionReceipt.GasUsed,
                Status = (ulong)(transactionSucceeded ? 1 : 0),
                Logs = jsonLogs,
            };

            // If we have a contract address, set it
            if (contractAddress != null)
            {
                jsonReceipt.ContractAddress = new Address(contractAddress.ToByteArray());
            }

            // Return our receipt.
            return Task.FromResult(jsonReceipt);
        }

        // Filters/Logs
        public Task<LogObjectResult> GetFilterChanges(ulong filterID)
        {
            // TODO: Implement
            throw new NotImplementedException();
        }

        public Task<LogObjectResult> GetFilterLogs(ulong filterID)
        {
            // If the filter id doesn't exist
            if (LogFilters.ContainsKey(filterID))
            {
                return Task.FromResult<LogObjectResult>(null);
            }

            // Obtain our filter
            FilterOptions filterOptions = LogFilters[filterID];

            // Obtain our logs.
            return GetLogs(filterOptions);
        }

        public Task<LogObjectResult> GetLogs(FilterOptions filterOptions)
        {
            // Obtain our filter address
            Meadow.EVM.Data_Types.Addressing.Address address = null;
            if (filterOptions.Address.HasValue)
            {
                address = new Meadow.EVM.Data_Types.Addressing.Address(filterOptions.Address.Value.GetBytes());
            }

            // We create our bloom filter for these filter options
            BigInteger filterBloom = 0;
            if (address != null)
            {
                filterBloom |= BloomFilter.Generate(address, Meadow.EVM.Data_Types.Addressing.Address.ADDRESS_SIZE);
            }

            if (filterOptions.Topics != null)
            {
                for (int i = 0; i < filterOptions.Topics.Length; i++)
                {
                    if (filterOptions.Topics[i] != null)
                    {
                        // Obtain our topic value.
                        BigInteger topicValue = BigIntegerConverter.GetBigInteger(filterOptions.Topics[i].Value.GetBytes());

                        // Add it to our bloom filter
                        filterBloom |= BloomFilter.Generate(topicValue);
                    }
                }
            }

            // Obtain our bounds.
            BigInteger startBlockNumber = BlockNumberFromBlockParameters(filterOptions.FromBlock);
            BigInteger endBlockNumber = BlockNumberFromBlockParameters(filterOptions.ToBlock);

            // Create our Log Object Result
            LogObjectResult result = new LogObjectResult();
            result.ResultType = LogObjectResultType.LogObjects;

            // Loop for all blocks
            List<FilterLogObject> jsonLogs = new List<FilterLogObject>();
            for (BigInteger i = startBlockNumber; i <= endBlockNumber; i++)
            {
                // Obtain the block at this index.
                var blockHash = TestChain.Chain.GetBlockHashFromBlockNumber(i);
                var block = TestChain.Chain.GetBlock(blockHash);

                // Verify the block's bloom filter with address and topics
                if (!BloomFilter.Check(block.Header.Bloom, filterBloom))
                {
                    continue;
                }

                // Obtain all the receipts for this block
                Meadow.EVM.Data_Types.Transactions.Transaction[] transactions = block.Transactions;
                Meadow.EVM.Data_Types.Transactions.Receipt[] receipts = State.GetReceipts(block, TestChain.Chain.Configuration.Database);

                // Loop through all the receipts
                for (int j = 0; j < receipts.Length; j++)
                {
                    // Obtain our receipt
                    var receipt = receipts[j];

                    // Verify the receipt's bloom filter with address and topics
                    if (!BloomFilter.Check(receipt.Bloom, filterBloom))
                    {
                        break;
                    }

                    // Obtain our transaction hash
                    byte[] transactionHash = transactions[j].GetHash();

                    // Loop through the logs
                    for (int x = 0; x < receipt.Logs.Count; x++)
                    {
                        // If the filter address exists, and our receipt address doesn't match, we skip.
                        if (address != null && address != receipt.Logs[x].Address)
                        {
                            continue;
                        }

                        // Next we'll want to filter the topics.
                        // TODO: The C# filter topic type might not support multiple-to-one matches, for the "OR" case. Revisit this.
                        bool skip = false;
                        for (int t = 0; t < filterOptions.Topics.Length && !skip; t++)
                        {
                            // If we have a filter for this argument.
                            if (filterOptions.Topics[t].HasValue)
                            {
                                // Obtain this indexed topic
                                byte[] topicData = BigIntegerConverter.GetBytes(receipt.Logs[x].Topics[t]);
                                if (!topicData.ValuesEqual(filterOptions.Topics[t].Value.GetBytes()))
                                {
                                    skip = true;
                                }
                            }
                        }

                        // If we should skip it, skip
                        if (skip)
                        {
                            continue;
                        }

                        // Otherwise we add this log to our log list.
                        jsonLogs.Add(JsonTypeConverter.CoreLogToJsonLog(receipt.Logs[x], x, (ulong)block.Header.BlockNumber, (ulong)j, new Hash(transactionHash), new Hash(blockHash)));
                    }
                }
            }

            // Set our result's log objects
            result.LogObjects = jsonLogs.ToArray();

            // Return our result
            return Task.FromResult(result);
        }

        public Task<ulong> NewFilter(FilterOptions filterOptions)
        {
            // Set our filter in our dictionary
            ulong filterId = _currentLogFilterId++;
            LogFilters[filterId] = filterOptions;

            // Return filter id
            return Task.FromResult(filterId);
        }

        public Task<bool> UninstallFilter(ulong filterId)
        {
            // Remove our filter id from our database
            bool removed = LogFilters.Remove(filterId);
            return Task.FromResult(removed);
        }

        // Snapshot/Revert
        public Task<ulong> Snapshot()
        {
            // Obtain our snapshot id and increment it.
            ulong snapshotId = _currentSnapshotId++;

            // Set our snapshot in our snapshot lookup.
            var snapshot = TestChain.Chain.State.Snapshot();
            var timeStampOffset = TestChain.Chain.Configuration.CurrentTimestampOffset;
            Snapshots[snapshotId] = (snapshot, timeStampOffset);

            // Return the snapshot id.
            return Task.FromResult(snapshotId);
        }

        public Task<bool> Revert(ulong snapshotID)
        {
            // Verify our snapshot ID is in our lookup
            if (Snapshots.TryGetValue(snapshotID, out var snapshotComponents))
            {
                // Revert the chain to this snapshot
                TestChain.Chain.Revert(snapshotComponents.snapshot);

                // Set our time stamp offset
                TestChain.Chain.Configuration.CurrentTimestampOffset = snapshotComponents.timeStampOffset;

                // Return our success status
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        // Cryptography
        public Task<byte[]> Sign(Address account, byte[] message)
        {
            // If our account isn't in our list of accounts, return null.
            if (!AccountDictionary.TryGetValue(account, out var keypair))
            {
                return null;
            }

            // Note: There are very many implementations of eth_sign across applications.
            // We base ours off of Geth, which apparently uses r,s,v.
            // (Source: https://github.com/paritytech/parity/issues/5490)
            // (More: https://github.com/ethereum/go-ethereum/pull/2940)

            // We create our prepended/generated message
            byte[] prependedMessage = UTF8Encoding.UTF8.GetBytes("\x19Ethereum Signed Message:\n");
            byte[] messageLength = UTF8Encoding.UTF8.GetBytes(message.Length.ToString(CultureInfo.InvariantCulture));

            // Calculate our hash from our concatted message
            byte[] hash = KeccakHash.ComputeHashBytes(prependedMessage.Concat(messageLength, message));

            // Sign the hash
            var signature = keypair.SignData(hash);

            // We want our result in r,s,v format.
            byte[] r = BigIntegerConverter.GetBytes(signature.r);
            byte[] s = BigIntegerConverter.GetBytes(signature.s);
            byte[] v = new byte[] { EthereumEcdsa.GetVFromRecoveryID(null, signature.RecoveryID) };

            // Concat all pieces together and return.
            byte[] result = r.Concat(s, v);
            return Task.FromResult(result);
        }

        public Task<Hash> Sha3(byte[] data)
        {
            // Compute a keccak 256 digest on the data.
            return Task.FromResult(new Hash(KeccakHash.ComputeHashBytes(data)));
        }

        // Unimplemented
        public Task<string> ProtocolVersion()
        {
            // TODO: Implement
            throw new NotImplementedException();
        }

        public Task<string> ClientVersion()
        {
            var runtime = Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;
            var version = GetType().Assembly.GetName().Version;
            var os = RuntimeInformation.OSDescription.Trim().Replace(' ', '_');
            var result = $"Meadow-TestServer/v{version}/{os}/{runtime}";
            return Task.FromResult(result);
        }

        public Task<SyncStatus> Syncing()
        {
            // TODO: Implement
            var result = new SyncStatus { IsSyncing = false, CurrentBlock = 999, HighestBlock = 2124, StartingBlock = 2 };
            return Task.FromResult(result);
        }

        public Task<string[]> GetCompilers()
        {
            // TODO: Implement
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

        public Task SetContractSizeCheckDisabled(bool enabled)
        {
            if (TestChain?.Chain?.Configuration?.DebugConfiguration != null)
            {
                TestChain.Chain.Configuration.DebugConfiguration.IsContractSizeCheckDisabled = enabled;
            }

            return Task.CompletedTask;
        }

        // Tracing
        public Task SetTracingEnabled(bool enabled)
        {
            // Set our coverage collection enabled status.
            if (TestChain?.Chain?.Configuration?.DebugConfiguration != null)
            {
                TestChain.Chain.Configuration.DebugConfiguration.IsTracing = enabled;
            }

            return Task.CompletedTask;
        }

        public Task<ExecutionTrace> GetExecutionTrace()
        {
            // Verify we have an execution trace.
            if (TestChain?.Chain?.Configuration?.DebugConfiguration.IsTracing != true || TestChain?.Chain?.Configuration?.DebugConfiguration.ExecutionTrace == null)
            {
                return Task.FromResult<ExecutionTrace>(null);
            }

            // Return our converted execution trace.
            ExecutionTrace executionTrace = JsonTypeConverter.CoreExecutionTraceToJsonExecutionTrace(TestChain.Chain.Configuration.DebugConfiguration.ExecutionTrace);
            return Task.FromResult(executionTrace);
        }

        // Coverage
        public Task SetCoverageEnabled(bool enabled)
        {
            // Set our coverage collection enabled status.
            if (TestChain?.Chain?.Configuration?.CodeCoverage != null)
            {
                TestChain.Chain.Configuration.CodeCoverage.Enabled = enabled;
            }

            return Task.CompletedTask;
        }

        public Task<CompoundCoverageMap> GetCoverageMap(Address contractAddress)
        {
            // Verify our coverage collection configuration exists.
            if (TestChain?.Chain?.Configuration?.CodeCoverage == null)
            {
                return Task.FromResult((CompoundCoverageMap)null);
            }

            // We try to obtain the code coverage map for this address.
            var coverageMap = TestChain.Chain.Configuration.CodeCoverage.Get(new Meadow.EVM.Data_Types.Addressing.Address(contractAddress.GetBytes()));

            // And return the coverage map
            CompoundCoverageMap result = JsonTypeConverter.CoreCompoundCoverageMapsToJsonCompoundCoverageMaps(coverageMap);
            return Task.FromResult(result);
        }

        public Task<CompoundCoverageMap[]> GetAllCoverageMaps()
        {
            // Verify our coverage collection configuration exists.
            if (TestChain?.Chain?.Configuration?.CodeCoverage == null)
            {
                return Task.FromResult(Array.Empty<CompoundCoverageMap>());
            }

            // We try to obtain all coverage maps.
            var coverageMaps = TestChain.Chain.Configuration.CodeCoverage.GetAll();
            if (coverageMaps == null)
            {
                return Task.FromResult(Array.Empty<CompoundCoverageMap>());
            }

            // Now we obtain internal data from that.
            CompoundCoverageMap[] result = new CompoundCoverageMap[coverageMaps.Length];
            for (int i = 0; i < result.Length; i++)
            {
                // Convert the coverage map to a json compatible format.
                result[i] = JsonTypeConverter.CoreCompoundCoverageMapsToJsonCompoundCoverageMaps(coverageMaps[i]);
            }

            return Task.FromResult(result);
        }

        public Task ClearCoverage()
        {
            // We'll want to clear all coverage maps.
            TestChain?.Chain?.Configuration?.CodeCoverage.Clear();
            return Task.CompletedTask;
        }

        public Task<bool> ClearCoverage(Address contractAddress)
        {
            // If we have no coverage, we stop and return that the contract wasn't in coverage.
            if (TestChain?.Chain?.Configuration?.CodeCoverage == null)
            {
                return Task.FromResult(false);
            }

            // We'll want to remove the coverage map for the given contract address.
            bool removed = TestChain.Chain.Configuration.CodeCoverage.Clear(new Meadow.EVM.Data_Types.Addressing.Address(contractAddress.GetBytes()));

            return Task.FromResult(removed);
        }

        // Helpers/Type Conversion
        /// <summary>
        /// Obtains the address which a contract is deployed to or will be deployed, having to do with the given transaction/call parameters.
        /// </summary>
        /// <param name="to">The address the transaction was sent to.</param>
        /// <param name="sender">The sender which sent the transaction.</param>
        /// <param name="blockHash">The hash of the block for which we'd like to obtain</param>
        /// <returns>Returns the address which the contract we are trying to interact with will be deployed to. Returns null if we are not trying to deploy an address.</returns>
        private Meadow.EVM.Data_Types.Addressing.Address GetDeployedAddress(Meadow.EVM.Data_Types.Addressing.Address to, Meadow.EVM.Data_Types.Addressing.Address sender, byte[] blockHash)
        {
            // Obtain our contract address (if we send this transaction to the create contract address)
            Meadow.EVM.Data_Types.Addressing.Address contractAddress = null;
            if (to == Meadow.EVM.Data_Types.Addressing.Address.CREATE_CONTRACT_ADDRESS)
            {
                // Obtain the state at this block number
                State state = TestChain.Chain.GetPostBlockState(blockHash);
                // Obtain the nonce from that
                BigInteger existingNonce = state.GetNonce(sender) - 1;
                contractAddress = GetDeployedAddress(to, sender, existingNonce);
            }

            return contractAddress;
        }

        /// <summary>
        /// Obtains the address which a contract is deployed to or will be deployed, having to do with the given transaction/call parameters.
        /// </summary>
        /// <param name="to">The address the transaction was sent to.</param>
        /// <param name="sender">The sender which sent the transaction.</param>
        /// <param name="senderNonceBeforeTransaction">The nonce of the sender before sending the transaction/call we are obtaining the address for.</param>
        /// <returns>Returns the address which the contract we are trying to interact with will be deployed to. Returns null if we are not trying to deploy an address.</returns>
        private Meadow.EVM.Data_Types.Addressing.Address GetDeployedAddress(Meadow.EVM.Data_Types.Addressing.Address to, Meadow.EVM.Data_Types.Addressing.Address sender, BigInteger senderNonceBeforeTransaction)
        {
            // Obtain our contract address (if we send this transaction to the create contract address)
            Meadow.EVM.Data_Types.Addressing.Address contractAddress = null;
            if (to == Meadow.EVM.Data_Types.Addressing.Address.CREATE_CONTRACT_ADDRESS)
            {
                contractAddress = Meadow.EVM.Data_Types.Addressing.Address.MakeContractAddress(sender, senderNonceBeforeTransaction);
            }

            return contractAddress;
        }

        /// <summary>
        /// Obtains information regarding if the transaction information supplied suggests we are deploying, our resulting deployed address (even if already deployed), 
        /// and the sender's nonce at the time of receiving this transaction.
        /// </summary>
        /// <param name="sender">The sender of the transaction to obtain transaction target information for.</param>
        /// <param name="to">The receiver of the transaction to obtain transaction target information for.</param>
        /// <returns>Obtains deploying status, our deployed address (even if already deployed), and the sender's nonce at the time of receiving this transaction.</returns>
        private (EVM.Data_Types.Addressing.Address targetDeployedAddress, bool deploying, BigInteger senderNonce) GetTransactionTargetInformation(EVM.Data_Types.Addressing.Address sender, EVM.Data_Types.Addressing.Address to)
        {
            // Obtain the sender's nonce.
            BigInteger senderNonce = TestChain.Chain.State.GetNonce(sender);

            // Obtain our deployed address
            var targetDeployedAddress = GetDeployedAddress(to, sender, senderNonce); // null if not deploying.
            bool deploying = targetDeployedAddress != null;
            targetDeployedAddress = targetDeployedAddress ?? to; // never null now, always refers to the address the contract will end up at.

            // Return our deployed information
            return (targetDeployedAddress, deploying, senderNonce);
        }

        /// <summary>
        /// Processes a given transaction, returning the resulting transaction hash if the transaction succeeds. If the transaction fails,
        /// an exception with the target/deployed contract address and deployed status is returned.
        /// </summary>
        /// <param name="transaction">The transaction to process on the test node.</param>
        /// <param name="deploying">True if this transaction will attempt to deploy a contract.</param>
        /// <param name="targetDeployedAddress">The address of the contract receiving the transaction, or if deploying a contract, the address the contract will deploy to.</param>
        /// <returns>Returns the transaction hash if the transaction succeeds, otherwise throws the causing exception with appropriate information attached.</returns>
        private Task<Hash> ProcessTransactionInternal(Meadow.EVM.Data_Types.Transactions.Transaction transaction, bool deploying, Meadow.EVM.Data_Types.Addressing.Address targetDeployedAddress)
        {
            // Queue the transaction for processing.
            TestChain.QueueTransaction(transaction);

            // Obtain our debug configuration error, and if it is set, throw the exception with relevant information.
            Exception ex = TestChain.Chain.Configuration.DebugConfiguration.Error;
            if (ex != null)
            {
                // Add our address to our exception, so we know which address execution traces pertain to.
                ex.Data["contract_address"] = new Address(targetDeployedAddress.ToByteArray());
                //ex.Data["contract_code"] = codeSegment.ToHexString(false);
                ex.Data["deploying"] = deploying;
                return Task.FromException<Hash>(ex);
            }

            // Return the transaction's hash
            return Task.FromResult(new Hash(transaction.GetHash()));
        }

        /// <summary>
        /// Executes a call on a given state, discarding all changes and returning the execution result.
        /// </summary>
        /// <param name="callParams">The call parameters to execute on the state.</param>
        /// <param name="state">The state to execute on (changes will be discared).</param>
        /// <returns>Returns the execution result of the call.</returns>
        private Meadow.EVM.EVM.Execution.EVMExecutionResult HandleCall(CallParams callParams, State state)
        {
            // Take a snapshot of our current state
            StateSnapshot snapshot = state.Snapshot();

            // Create our default parameters
            Meadow.EVM.Data_Types.Addressing.Address to = new Meadow.EVM.Data_Types.Addressing.Address(0);
            if (callParams.To.HasValue)
            {
                to = new Meadow.EVM.Data_Types.Addressing.Address(callParams.To.Value.GetBytes());
            }

            Meadow.EVM.Data_Types.Addressing.Address from = new Meadow.EVM.Data_Types.Addressing.Address(0);
            if (callParams.From.HasValue)
            {
                from = new Meadow.EVM.Data_Types.Addressing.Address(callParams.From.Value.GetBytes());
            }

            // Constructor a transaction from our provided parameters.
            BigInteger existingNonce = TestChain.Chain.State.GetNonce(new Meadow.EVM.Data_Types.Addressing.Address(callParams.From.Value.GetBytes()));
            Meadow.EVM.EVM.Messages.EVMMessage message = new Meadow.EVM.EVM.Messages.EVMMessage(
                from,
                to,
                (BigInteger?)callParams.Value ?? 0,
                (BigInteger?)callParams.Gas ?? GasDefinitions.CalculateGasLimit(TestChain.Chain.GetHeadBlock().Header, TestChain.Chain.Configuration),
                callParams.Data ?? Array.Empty<byte>(),
                0,
                to,
                true,
                false);

            // Execute and obtain our result
            Meadow.EVM.EVM.Execution.EVMExecutionResult result = MeadowEVM.Execute(state, message);

            // Revert to our initial state so no changes were made.
            TestChain.Chain.State.Revert(snapshot);

            // Return the execution result.
            return result;
        }

        /// <summary>
        /// Obtains the state for a given period in time, determined by the block parameter. Warning: This could be an old copy of State, or it could be the current instance of state.
        /// </summary>
        /// <param name="blockParameter">The block parameters which dictate the point in time for which we wish to obtain data.</param>
        /// <returns>Returns either a copy of an previous state, or the current instance of our state.</returns>
        private State GetStateFromBlockParameters(DefaultBlockParameter blockParameter)
        {
            // If we want the latest, we return the current state
            if (blockParameter.ParameterType == BlockParameterType.Latest)
            {
                return TestChain.Chain.State;
            }

            // We want to obtain the block for the block number we wish to process.
            BigInteger blockNumber = BlockNumberFromBlockParameters(blockParameter);
            byte[] blockHash = TestChain.Chain.GetBlockHashFromBlockNumber(blockNumber);
            if (blockHash == null)
            {
                return null;
            }

            // If we could obtain the block, we want the state after this block
            State postBlockState = TestChain.Chain.GetPostBlockState(blockHash);
            if (postBlockState == null)
            {
                return null;
            }

            // Return our post block state
            return postBlockState;
        }

        /// <summary>
        /// Obtains the block number for a given period in time, determined by the block parameter.
        /// </summary>
        /// <param name="blockParameter">The block parameters which dictate the point in time for which we wish to obtain data.</param>
        /// <returns>Returns the block number derived from the block parameter.</returns>
        private BigInteger BlockNumberFromBlockParameters(DefaultBlockParameter blockParameter)
        {
            // Determine block number dependent on block parameters.
            if (blockParameter.ParameterType == BlockParameterType.BlockNumber)
            {
                // Set the block number as the provided block number.
                return (BigInteger)blockParameter.BlockNumber;
            }
            else if (blockParameter.ParameterType == BlockParameterType.Earliest)
            {
                // Set the block number as the genesis block.
                return TestChain.Chain.Configuration.GenesisBlock.Header.BlockNumber;
            }
            else if (blockParameter.ParameterType == BlockParameterType.Latest)
            {
                // Set the block number as the head block.
                return TestChain.Chain.GetHeadBlock().Header.BlockNumber;
            }
            else if (blockParameter.ParameterType == BlockParameterType.Pending)
            { 
                // TODO: verify this..
                // Test node instantly mine blocks so there is no pending block, use latest block instead
                return TestChain.Chain.GetHeadBlock().Header.BlockNumber;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        public void Dispose()
        {
            RpcServer.Dispose();
        }

    }
}
