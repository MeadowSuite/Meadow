using Meadow.EVM.Data_Types.Accounts;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Data_Types.Transactions;
using Meadow.EVM.EVM.Messages;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Meadow.EVM.Configuration;
using Meadow.EVM.Exceptions;
using Meadow.EVM.EVM.Execution;
using Meadow.EVM.Data_Types.Trees;
using Meadow.EVM.EVM.Definitions;
using System.Reflection;
using Meadow.EVM.Data_Types.Block;
using Meadow.EVM.Data_Types.Databases;
using Meadow.EVM.Data_Types.Chain;
using Meadow.EVM.Data_Types.Trees.Comparer;
using Meadow.Core.Utils;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Cryptography;
using Meadow.Core.RlpEncoding;

namespace Meadow.EVM.Data_Types.State
{
    public class State : ICloneable
    {
        #region Properties
        /// <summary>
        /// Defines the configuration for the Ethereum implementation, including fork/version implementation, etc.
        /// </summary>
        public Configuration.Configuration Configuration { get; set; }
        /// <summary>
        /// The state trie which contains all accounts which can be looked up by address.
        /// </summary>
        public Trie Trie { get; set; }

        /// <summary>
        /// Indicates the amount of gas used by this state while processing a block.
        /// </summary>
        public BigInteger BlockGasUsed { get; set; }
        /// <summary>
        /// The index of the last transaction processed, beginning from the start of this state/chain.
        /// </summary>
        public BigInteger TransactionIndex { get; set; }
        /// <summary>
        /// Indicates the amount that should be refunded after this transaction is applied. This can occur during many occasions such as a contract self destructing.
        /// </summary>
        public BigInteger TransactionRefunds { get; set; }
        /// <summary>
        /// The list of logs for the current transaction application/message execution.
        /// </summary>
        public List<Log> TransactionLogs { get; set; }
        /// <summary>
        /// Receipts for this state's last applied block.
        /// </summary>
        public List<Receipt> TransactionReceipts { get; set; }
        /// <summary>
        /// Bloom filter which incompasses all indexable information.
        /// </summary>
        public BigInteger Bloom { get; set; }

        // Accounts
        /// <summary>
        /// Account cache which is used for accounts which we obtained/modified since our last state commit.
        /// </summary>
        public Dictionary<Address, Account> CachedAccounts { get; set; }
        /// <summary>
        /// Accounts are deleted after the transaction has completed, so we queue up accounts to delete until then.
        /// </summary>
        public HashSet<Address> AccountDeleteQueue { get; set; }

        /// <summary>
        /// Previous headers processed by our state, where the the earlier the item in the array, the most recent it is.
        /// </summary>
        public List<Block.BlockHeader> PreviousHeaders { get; set; }
        /// <summary>
        /// The current or most recent block which is processing/was processed in this state.
        /// </summary>
        public Block.Block CurrentBlock { get; private set; }
        /// <summary>
        /// The current or most recent transaction which this state had applied/attempted to apply.
        /// </summary>
        public Transactions.Transaction CurrentTransaction { get; set; }
        /// <summary>
        /// A cache of recent uncle block hashes to be referenced for future block uncle candidate selection.
        /// Maps block number -> block header hash value.
        /// </summary>
        public Dictionary<BigInteger, byte[][]> RecentUncleHashes { get; set; } // block num->uncle header hashes
        #endregion

        #region Constructors
        /// <summary>
        /// The default constructor for a state, initializes it with a given optional configuration (and root node hash if restoring state).
        /// </summary>
        /// <param name="configuration">(Optional) The configuration to use for this state object. If one is not supplied, a new configuration is created.</param>
        /// <param name="rootNodeHash">(Optional) The root node hash for our state trie (accounts, which in turn also hold their own storage tries) used to restore state.</param>
        public State(Configuration.Configuration configuration = null, byte[] rootNodeHash = null)
        {
            // If a configuration wasn't provided, create a default one.
            if (configuration == null)
            {
                configuration = new Configuration.Configuration();
            }

            // Set our properties
            Configuration = configuration;
            PreviousHeaders = new List<Block.BlockHeader>();
            RecentUncleHashes = new Dictionary<BigInteger, byte[][]>();
            TransactionLogs = new List<Log>();
            TransactionReceipts = new List<Receipt>();
            CachedAccounts = new Dictionary<Address, Account>();
            AccountDeleteQueue = new HashSet<Address>();

            // Set up our trie with our given root hash
            Trie = new Trie(configuration.Database, rootNodeHash);

            // Update our state now that we've updated the current block.
            Configuration.UpdateEthereumRelease(this);
        }
        #endregion

        #region Functions

        #region State Transitions (State Setup)
        /// <summary>
        /// Sets the state to use the provided block as the current block, updating uncle hashes and fork version.
        /// </summary>
        /// <param name="currentBlock">The block to make state use as the current block.</param>
        public void UpdateCurrentBlock(Block.Block currentBlock)
        {
            // Update our uncle hashes
            UpdateUncleHashes(currentBlock);

            // Set our current block.
            CurrentBlock = currentBlock;

            // Update our configuration version
            Configuration.UpdateEthereumRelease(this);
        }

        /// <summary>
        /// Updates the state's recent block uncles collection with uncles from the given block.
        /// </summary>
        /// <param name="blockWithUncles">The block whose uncles we use to update our current state's recent uncles list.</param>
        public void UpdateUncleHashes(Block.Block blockWithUncles)
        {
            // Update our uncle hashes
            RecentUncleHashes[blockWithUncles.Header.BlockNumber] = new byte[blockWithUncles.Uncles.Length][];
            for (int i = 0; i < blockWithUncles.Uncles.Length; i++)
            {
                RecentUncleHashes[blockWithUncles.Header.BlockNumber][i] = blockWithUncles.Uncles[i].GetHash();
            }
        }
        #endregion

        #region Tries (Transaction/Receipts)
        /// <summary>
        /// Generates a trie for transactions/transaction receipts given an array of them, and an optional database to use for the trie.
        /// </summary>
        /// <param name="serializableItems">An array of RLP serializable items (transactions/transaction receipts) to construct a trie of, where each item's key is an RLP serialized integer index.</param>
        /// <param name="database">The optional database to use for the trie. If null, a new database is used for the Trie.</param>
        /// <returns>Returns a trie generated from the provided RLP serializable items.</returns>
        private static Trie GenerateTransactionTrie(IRLPSerializable[] serializableItems, BaseDB database = null)
        {
            // We create a new trie for these items
            Trie trie = new Trie(database);

            // Loop for each item and set it in our database.
            for (int i = 0; i < serializableItems.Length; i++)
            {
                trie.Set(RLP.Encode(RLP.FromInteger(i, EVMDefinitions.WORD_SIZE, true)), RLP.Encode(serializableItems[i].Serialize()));
            }

            // Return our trie
            return trie;
        }

        /// <summary>
        /// Obtain all transaction receipts for a given block from the given database.
        /// </summary>
        /// <param name="block">The block to obtain transaction receipts for.</param>
        /// <param name="database">The database to obtain transaction receipts from.</param>
        /// <returns>Returns the array of transaction receipts for the transactions in the provided block.</returns>
        public static Receipt[] GetReceipts(Block.Block block, BaseDB database)
        {
            // Obtain our receipts.
            return GetReceipts(block.Header, block.Transactions.Length, database);
        }

        /// <summary>
        /// Obtain the given amount of transaction receipts for a given block header from the given database.
        /// </summary>
        /// <param name="blockHeader">The block header to obtain transaction receipts for.</param>
        /// <param name="transactionCount">The amount of transactions to obtain for the given block header.</param>
        /// <param name="database">The database to obtain transaction receipts from.</param>
        /// <returns>Returns the array of transaction receipts for the transactions in the provided block header.</returns>
        public static Receipt[] GetReceipts(BlockHeader blockHeader, int transactionCount, BaseDB database)
        {
            // Obtain the trie using the root node hash
            Trie receiptTrie = new Trie(database, blockHeader.ReceiptsRootHash);

            // Initialize our receipt array.
            Receipt[] receipts = new Receipt[transactionCount];
            for (int i = 0; i < receipts.Length; i++)
            {
                // Create our key from our transaction index
                byte[] key = RLP.Encode(RLP.FromInteger(i, EVMDefinitions.WORD_SIZE, true));

                // Obtain our value (transaction receipt)
                byte[] receiptValue = receiptTrie.Get(key);
                if (receiptValue == null)
                {
                    return null;
                }

                // Create a new receipt instance from our RLP decoded value
                Receipt transactionReceipt = new Receipt(RLP.Decode(receiptValue));
                receipts[i] = transactionReceipt;
            }

            return receipts;
        }

        /// <summary>
        /// Obtain a transaction receipt at the given index for a given block header from the given database.
        /// </summary>
        /// <param name="blockHeader">The block header which owns the desired transaction receipt.</param>
        /// <param name="transactionIndex">The index of the desired transaction in the provided block.</param>
        /// <param name="database">The database to obtain transaction receipts from.</param>
        /// <returns>Returns the transaction receipt at the given index for the transactions in the provided block header.</returns>
        public static Receipt GetReceipt(BlockHeader blockHeader, int transactionIndex, BaseDB database)
        {
            // Obtain the trie using the root node hash
            Trie receiptTrie = new Trie(database, blockHeader.ReceiptsRootHash);

            // Create our key from our transaction index
            byte[] key = RLP.Encode(RLP.FromInteger(transactionIndex, EVMDefinitions.WORD_SIZE, true));

            // Obtain our value (transaction receipt)
            byte[] receiptValue = receiptTrie.Get(key);
            if (receiptValue == null)
            {
                return null;
            }

            // Create a new receipt instance from our RLP decoded value
            Receipt transactionReceipt = new Receipt(RLP.Decode(receiptValue));
            return transactionReceipt;
        }
        #endregion

        #region State Transition (Block Mining)
        /// <summary>
        /// Given a chain, and parent/last block to build on/fork from, creates a new successor/head block from transactions in the given pool,
        /// and returns the post-execution state for the new block.
        /// </summary>
        /// <param name="chain">The chain which owns the <paramref name="parentHeader"/> block header, to build a new head candidate for.</param>
        /// <param name="transactionPool">The pool of queued transactions to pull transactions from for the new block candidate.</param>
        /// <param name="parentHeader">The desired parent block header for our new head block. Must be a block header within the provided chain.</param>
        /// <param name="timestamp">The timestamp to attach to the new block.</param>
        /// <param name="coinbase">The coinbase/miner address for the block.</param>
        /// <param name="extraData">The extra data to include in the block.</param>
        /// <param name="minimumGasPrice">The minimum gas price to use when pulling transactions from the transaction pool.</param>
        /// <returns>Returns the newly created head block for the given chain/desired parent using transactions from the transaction pool, as well as the post-execution state for it.</returns>
        public (Block.Block newBlock, State newState) CreateNewHeadCandidate(Chain.PoW.ChainPoW chain, TransactionPool transactionPool, BlockHeader parentHeader, BigInteger timestamp, Address coinbase, byte[] extraData, BigInteger minimumGasPrice)
        {
            // If we didn't offer a parent block, we simply clone the current chain state, otherwise we derive the state to begin on
            State newState = null;
            if (parentHeader == null)
            {
                // Clone the state.
                newState = (State)Clone();
            }
            else
            {
                // Derive the point to begin at by obtaining the state after the parent block had executed.
                newState = chain.GetPostBlockState(parentHeader.GetHash());
            }

            // Determine some block properties
            BigInteger newBlockNumber = parentHeader.BlockNumber + 1;
            BigInteger newBlockDifficulty = Block.Block.CalculateDifficulty(parentHeader, timestamp, Configuration);
            BigInteger newGasLimit = GasDefinitions.CalculateGasLimit(parentHeader, Configuration);

            // Create a template block header for our new block, filling what properties we can at this point.
            BlockHeader newBlockHeader = new BlockHeader(
                parentHeader.GetHash(), // previous block hash
                null, // uncle hash
                coinbase, // coinbase/miner address
                null, // state root hash
                null, // transaction root hash
                null, // receipts root hash
                0, // bloom
                newBlockDifficulty, // difficulty
                newBlockNumber, // block number
                newGasLimit, // gas limit
                0, // gas used
                timestamp, // timestamp
                extraData ?? Array.Empty<byte>(), // extra data
                null, // mix hash
                null); // nonce

            // Put the block header into a block.
            Block.Block newBlock = new Block.Block(newBlockHeader, Array.Empty<Transaction>(),  Array.Empty<BlockHeader>());

            // Set our blocks uncles
            newBlock.Uncles = Configuration.Consensus.GetUncleCandidates(chain, this).ToArray();
            newBlock.Header.UpdateUnclesHash(newBlock.Uncles); // update uncles hash

            // Now that we have the block and its pre-application state, we use the current consensus mechanism
            // to initialize/finalize our block and state, applying transactions from the queue/transaction pool
            // and setting the results in the new state.

            // Go into initialization state
            Configuration.Consensus.Initialize(newState, newBlock);

            // Add transactions from our pool
            AddTransactions(newState, newBlock, transactionPool, minimumGasPrice);

            // Enter our finalization state
            Configuration.Consensus.Finalize(newState, newBlock);

            // Set our block's execution results
            newState.SetExecutionResults(newBlock);

            // Do any post-finalization to our state
            newState.PreviousHeaders.Insert(0, newBlock.Header);

            // IMPORTANT: Because we have a shared 'configuration', we make sure we update the configuration with our original state again (TODO: Revisit this, maybe duplicate configuration!)
            Configuration.UpdateEthereumRelease(this);

            // At this point, state applied all transactions that were included in the block and has transitioned
            // from a pre-block application state to a post-block application state. The block is also populated
            // with properties from the post-block execution state we created.
            // (Block is missing the mix hash/nonce values, which have to be mined for).

            // Return the new state and block.
            return (newBlock, newState);
        }

        /// <summary>
        /// Applies transactions from the given transaction pool (<paramref name="transactionPool"/>) which meet the minimum gas requirement 
        /// (<paramref name="minimumGasPrice"/>) to the provided state (<paramref name="state"/>) and adds them to a new block's transaction
        /// collection.
        /// </summary>
        /// <param name="state">The state which we wish to process the picked transactions from the pool on, to obtain block values for.</param>
        /// <param name="block">The block to insert the transactions picked from the transaction pool into.</param>
        /// <param name="transactionPool">The pool of queued transactions to pull transactions from for the new block.</param>
        /// <param name="minimumGasPrice">The minimum gas price to use when pulling transactions from the transaction pool.</param>
        private static void AddTransactions(State state, Block.Block block, TransactionPool transactionPool, BigInteger minimumGasPrice)
        {
            // If our pool is empty, there's nothing to do.
            if (transactionPool == null || transactionPool.Count == 0)
            {
                return;
            }

            // Keep adding transactions
            List<Transaction> newTransactions = new List<Transaction>();
            while (true)
            {
                // Pop a transaction off.
                Transaction transaction = transactionPool.Pop(minimumGasPrice, state.CurrentBlock.Header.GasLimit - state.BlockGasUsed);

                try
                {
                    // If we have a valid item, we process it.
                    if (transaction != null)
                    {
                        // Apply our transaction
                        state.ApplyTransaction(transaction);
                        // Add our transaction.
                        newTransactions.Add(transaction);
                    }
                    else
                    {
                        break;
                    }
                }
                catch { break; }
            }

            // Add to our block's transaction list.
            block.Transactions = block.Transactions.Concat(newTransactions.ToArray());
        }
        #endregion

        #region State Transition (Block Applying)
        /// <summary>
        /// Performs preliminary block verification before applying the block to this state.
        /// Throws a <see cref="BlockException"/> exception if verification fails.
        /// </summary>
        /// <param name="blockHeader">The block header to verify prior to applying the block.</param>
        private void VerifyBlock(BlockHeader blockHeader)
        {
            // Check we have a parent to verify.
            if (PreviousHeaders.Count > 0 && PreviousHeaders[0] != null)
            {
                // Obtain our parent block header.
                BlockHeader parentBlockHeader = PreviousHeaders[0];

                // Verify our hashes match
                if (!blockHeader.PreviousHash.ValuesEqual(parentBlockHeader.GetHash()))
                {
                    throw new BlockException("Block verification encountered a previous hash mismatch with the state's previous block hash.");
                }

                // Verify the blocks number is sequentially following
                if (blockHeader.BlockNumber != parentBlockHeader.BlockNumber + 1)
                {
                    throw new BlockException("Block verification found a non-sequential block number between this block and its parent.");
                }

                // Verify the gas limit adjustment
                bool validGasLimitAdjustment = GasDefinitions.CheckGasLimit(parentBlockHeader.GasLimit, blockHeader.GasLimit, Configuration);
                if (!validGasLimitAdjustment)
                {
                    throw new BlockException("Block verification found gas limit adjustment between this block and the parent do not meet configuration requirements.");
                }

                // Verify the difficulty for this block matches our calculated difficulty.
                BigInteger calculatedDifficulty = Block.Block.CalculateDifficulty(parentBlockHeader, blockHeader.Timestamp, Configuration);
                if (blockHeader.Difficulty != calculatedDifficulty)
                {
                    throw new BlockException($"Block verification encountered a calculated difficulty mismatch. Block = {blockHeader.Difficulty}, Calculated = {calculatedDifficulty}");
                }

                // Verify the gas used does not exceed the limit
                if (blockHeader.GasUsed > blockHeader.GasLimit)
                {
                    throw new BlockException("Block verification found gas used exceeded gas limit.");
                }

                // Verify our extra data is not too long.
                if (blockHeader.ExtraData.Length > 1024 && (blockHeader.ExtraData.Length > 32 && Configuration.Version < EthereumRelease.WIP_Serenity))
                {
                    throw new BlockException("Block verification found extra data exceeded max length!");
                }

                // Verify our time stamp comes after our parents
                if (blockHeader.Timestamp < parentBlockHeader.Timestamp)
                {
                    throw new BlockException("Block verification found timestamp didn't come after its parent's timestamp.");
                }

                // Verify our timestamp isn't too large
                if (blockHeader.Timestamp > EVMDefinitions.UINT256_MAX_VALUE)
                {
                    throw new BlockException("Block verification found timestamp was too large. (Exceeded 256-bits).");
                }

                // Verify our gas limit doesn't exceed our max gas limit.
                if (blockHeader.GasLimit > Configuration.MaxGasLimit)
                {
                    throw new BlockException("Block verification found gas limit exceeded the maximum.");
                }

                // Before the DAO fork, there should be extra DAO fork data.
                BigInteger blocksUntilDAO = blockHeader.BlockNumber - Configuration.GetReleaseStartBlockNumber(EthereumRelease.DAO);
                if (blocksUntilDAO >= 0 && blocksUntilDAO < 10 && blockHeader.ExtraData != null && !blockHeader.ExtraData.ValuesEqual(Configuration.DAOForkBlockExtraData))
                {
                    throw new BlockException("0-10 blocks pre-DAO fork should include special DAO extra data!");
                }
            }
        }

        /// <summary>
        /// Performs execution result verification after applying a block to this state.
        /// </summary>
        /// <param name="block">The block to verify the values against after applying the block to this state.</param>
        private void VerifyExecutionResults(Block.Block block)
        {
            // Verify our state's bloom matches our blocks.
            if (Bloom != block.Header.Bloom)
            {
                throw new ArgumentException($"Bloom filter mismatch between state and block. State ({Bloom}), Block({block.Header.Bloom}).");
            }

            // If bloom matched, our results likely matched, so we can commit.
            CommitChanges();

            // Now that we committed, we verify our the hashes of all of our trie roots.

            // Verify state root hash
            if (!block.Header.StateRootHash.ValuesEqual(Trie.GetRootNodeHash()))
            {
                throw new ArgumentException("State root hash mismatch between state which applied the block, and the block header itself when verifying execution results.");
            }

            // Verify receipt root hash
            Receipt[] receiptArray = TransactionReceipts.ToArray();
            byte[] transactionTrieRootHash = GenerateTransactionTrie(receiptArray, Configuration.Database).GetRootNodeHash();
            if (!transactionTrieRootHash.ValuesEqual(block.Header.ReceiptsRootHash))
            {
                throw new ArgumentException("Receipt trie root hash mismatch when verifying execution results!");
            }

            // Verify our gas used matched in the state and header
            if (BlockGasUsed != block.Header.GasUsed)
            {
                throw new ArgumentException("Gas used mismatch between state and block when verifying execution results.");
            }
        }

        /// <summary>
        /// Sets the execution results in the given block's header from this state's variables (post-transaction application).
        /// </summary>
        /// <param name="block">The block to set the values in after applying the transactions the block will have to this state.</param>
        public void SetExecutionResults(Block.Block block)
        {
            // We'll want to commit any uncommitted changes.
            CommitChanges();

            // Now we can copy over our post-execution results
            Receipt[] receiptArray = TransactionReceipts.ToArray();
            block.Header.ReceiptsRootHash = GenerateTransactionTrie(receiptArray, Configuration.Database).GetRootNodeHash();

            // Generate the transactions root.
            block.Header.TransactionsRootHash = GenerateTransactionTrie(block.Transactions, Configuration.Database).GetRootNodeHash();

            // Set the state root
            block.Header.StateRootHash = Trie.GetRootNodeHash();

            // Set our bloom
            block.Header.Bloom = Bloom;

            // Set our gas used
            block.Header.GasUsed = BlockGasUsed;
        }

        /// <summary>
        /// Applies a given block to the state, performing all necessary checks/updates, and executing all underlying transactions.
        /// </summary>
        /// <param name="block">The block to apply to this state.</param>
        public void ApplyBlock(Block.Block block)
        {
            // Take a snapshot of our state before processing our block.
            StateSnapshot snapshot = Snapshot();

            // Try to apply our block with our given consensus strategy.
            try
            {
                // Initialize state transition to handle this block.
                Configuration.Consensus.Initialize(this, block);

                // Verify our block
                VerifyBlock(block.Header);

                // Verify the proof of work/stake/etc consensus method.
                if (!Configuration.Consensus.CheckProof(this, block.Header))
                {
                    throw new BlockException("Consensus mechanism's proof has failed!");
                }

                // Next we validate our uncles
                if (!Configuration.Consensus.VerifyUncles(this, block))
                {
                    throw new BlockException("ApplyBlock failed because uncles failed validation under consensus mechanism!");
                }

                // Validate our transaction tree
                byte[] transactionTrieRootHash = GenerateTransactionTrie(block.Transactions, Configuration.Database).GetRootNodeHash();
                if (!transactionTrieRootHash.ValuesEqual(block.Header.TransactionsRootHash))
                {
                    throw new BlockException("Transaction trie root hash mismatch when applying block!");
                }

                // Process all transactions
                foreach (Transaction transaction in block.Transactions)
                {
                    ApplyTransaction(transaction);
                }

                // Finalize state transition with our consensus mechanism
                Configuration.Consensus.Finalize(this, block);

                // Verify execution results.
                VerifyExecutionResults(block);

                // Do any post-finalization to our state
                PreviousHeaders.Insert(0, block.Header);
            }
            catch
            {
                // An exception occurred, we revert and throw the exception again.
                Revert(snapshot);
                throw;
            }
        }
        #endregion

        #region State Transition (Transaction Applying)
        /// <summary>
        /// Performs preliminary transaction verification before applying the transaction to this state.
        /// </summary>
        /// <param name="transaction">The transaction to verify prior to applying to this state.</param>
        private void VerifyTransaction(Transaction transaction)
        {
            // Verify the transaction signature is valid
            // Try to obtain the sender address (verifies the signature is valid)
            Address senderAddress = transaction.GetSenderAddress();

            if (Configuration.Version >= EthereumRelease.WIP_Constantinople)
            {
                // Enforce low s
                if (!Secp256k1Curve.CheckLowS(transaction.ECDSA_s))
                {
                    throw new TransactionException("Invalid S parameter in transaction signature (too high)!");
                }
            }
            else
            {
                // A transaction cannot go to zero address if EIP86 isn't implemented yet.
                if (senderAddress == Address.ZERO_ADDRESS)
                {
                    throw new TransactionException("Cannot send to zero address since EIP86 is not implemented yet.");
                }

                // If we're past homestead, we enforce low S
                if (Configuration.Version >= EthereumRelease.Homestead)
                {
                    // Enforce low S and non-zero S.
                    if (!Secp256k1Curve.CheckLowS(transaction.ECDSA_s) || transaction.ECDSA_s == 0)
                    {
                        throw new TransactionException("Invalid S parameter in transaction signature!");
                    }
                }
            }

            // We verify chain ID past spurious dragon
            if (Configuration.Version >= EthereumRelease.SpuriousDragon)
            {
                // If our chain ID on our transaction doesn't match our configuration's network ID, we throw an exception.
                if (transaction.ChainID != null && transaction.ChainID != Configuration.ChainID)
                {
                    throw new TransactionException($"ChainID mismatch between the transaction and the current configuration. Configuration ChainID = {Configuration.ChainID}, Transaction ChainID = {transaction.ChainID}");
                }
            }
            else
            {
                // Before spurious dragon, chain ID was not embedded in the v parameter. It should be null.
                if (transaction.ChainID != null)
                {
                    throw new TransactionException($"ChainID mismatch between the transaction and the current configuration. Configuration ChainID = {Configuration.ChainID}, Transaction ChainID = {transaction.ChainID}");
                }
            }


            // Obtain the sender nonce and verify it matches the transaction nonce
            BigInteger senderNonce = 0;
            if (senderAddress != Address.NULL_ADDRESS)
            {
                senderNonce = GetNonce(senderAddress);
            }

            if (senderNonce != transaction.Nonce)
            {
                throw new TransactionException($"Nonce mismatch between the transaction and its sender. Transaction nonce = {transaction.Nonce}. Sender nonce = {senderNonce}");
            }

            // If our transaction start gas is less than our base cost, throw an exception
            if (transaction.StartGas < transaction.BaseGasCost)
            {
                throw new TransactionException($"Transaction did not have enough start gas ({transaction.StartGas}) to cover the base cost of this CREATE transaction ({transaction.BaseGasCost}).");
            }

            // Verify our sender's balance is enough to pay for this transaction.
            BigInteger senderBalance = GetBalance(senderAddress);
            BigInteger transactionTotalCost = transaction.Value + (transaction.StartGas * transaction.GasPrice);
            if (senderBalance < transactionTotalCost)
            {
                throw new TransactionException($"Transaction sender did not have enough balance ({senderBalance}) to pay for the transaction gas price ({transactionTotalCost}).");
            }

            // Verify that our block gas limit has not been exceeded.
            if (BlockGasUsed + transaction.StartGas > CurrentBlock.Header.GasLimit)
            {
                throw new TransactionException($"Reached block gas limit when applying transaction.");
            }

            // If the transaction sender is the null address, the gasprice and value must be 0 (EIP-86)
            if (senderAddress == Address.NULL_ADDRESS)
            {
                if (transaction.Value != 0)
                {
                    throw new TransactionException("Transaction value cannot be non-zero when sending to null address.");
                }
                else if (transaction.GasPrice != 0)
                {
                    throw new TransactionException("Transaction gas price cannot be non-zero when sending to null address.");
                }
            }

        }

        /// <summary>
        /// Applies a given transaction to the state, performing all necessary checks/updates.
        /// </summary>
        /// <param name="transaction">The transaction to apply to this state.</param>
        public void ApplyTransaction(Transaction transaction)
        {
            // Verify our transaction
            VerifyTransaction(transaction);

            // Set the current transaction
            CurrentTransaction = transaction;

            // Clear our state up to apply these logs.
            TransactionRefunds = 0;
            TransactionLogs.Clear();
            AccountDeleteQueue.Clear();

            // Try to obtain the sender address (verifies the signature is valid)
            Address senderAddress = transaction.GetSenderAddress();

            // Obtain our transaction cost
            BigInteger transactionBaseCost = transaction.BaseGasCost;

            // If we are past the homestead fork and we're creating an account, we ensure we have enough starting gas based off of our intrinsic gas value.
            if (Configuration.Version >= EthereumRelease.Homestead && transaction.To == Address.CREATE_CONTRACT_ADDRESS)
            {
                // Add our CREATE opcode cost to our transaction since we're creating an account. We can safely cast the nullable type here since we know this instruction has always existed.
                transactionBaseCost += (uint)GasDefinitions.GetInstructionBaseGasCost(Configuration.Version, EVM.Instructions.InstructionOpcode.CREATE);

                // If we don't have enough gas to cover this transaction, we throw an exception.
                if (transaction.StartGas < transactionBaseCost)
                {
                    throw new TransactionException($"Transaction did not have enough start gas ({transaction.StartGas}) to cover the base cost of this CREATE transaction ({transactionBaseCost}).");
                }
            }

            // Charge the sender for the transaction start gas now.
            ModifyBalanceDelta(senderAddress, -transaction.StartGas * transaction.GasPrice);

            // If the sender is anyone but the null address, increment nonce.
            if (senderAddress != Address.NULL_ADDRESS)
            {
                IncrementNonce(senderAddress);
            }

            // Next we create a message from this transaction to execute (note: we remove the transaction base cost at this point as well too).
            EVMMessage message = new EVMMessage(senderAddress, transaction.To, transaction.Value, transaction.StartGas - transactionBaseCost, transaction.Data, 0, transaction.To, true, false);

            // Determine if we're creating a contract or executing in our virtual machine.
            EVMExecutionResult executionResult = null;
            if (transaction.To == Address.CREATE_CONTRACT_ADDRESS)
            {
                // Create a contract
                executionResult = MeadowEVM.CreateContract(this, message);
            }
            else
            {
                // Execute code
                executionResult = MeadowEVM.Execute(this, message);
            }


            // The amount of gas used in this transaction thus far.
            BigInteger transactionGasUsed = transaction.StartGas - executionResult.RemainingGas;
            BigInteger transactionGasRemaining = executionResult.RemainingGas;

            // Determine how our execution went.
            if (executionResult.Succeeded)
            {
                // If we had accounts which are queued up to be deleted, we offer a refund for each.
                TransactionRefunds += AccountDeleteQueue.Count * GasDefinitions.GAS_SELF_DESTRUCT_REFUND;

                // If we have any gas to refund
                if (TransactionRefunds > 0)
                {
                    // We refund the gas in our refund amount, capped to no more than half of the gas used total.
                    TransactionRefunds = BigInteger.Min(TransactionRefunds, transactionGasUsed / 2);
                    transactionGasRemaining += TransactionRefunds;
                    transactionGasUsed -= TransactionRefunds;

                    // Now that we've refunded, we set our refund counter to zero.
                    TransactionRefunds = 0;
                }
            }

            // Update the amount of gas used in our state.
            BlockGasUsed += transactionGasUsed;

            // We return our remaining gas to the sender, and assign the award (our used gas) for this transaction to the coinbase.
            ModifyBalanceDelta(senderAddress, transaction.GasPrice * transactionGasRemaining);
            ModifyBalanceDelta(CurrentBlock.Header.Coinbase, transaction.GasPrice * transactionGasUsed);

            // Next we'll want to delete all self destructed accounts/queued deletes.
            foreach (Address queuedDeleteAddress in AccountDeleteQueue)
            {
                // We set the balance of the address to zero and delete it immediately.
                SetBalance(queuedDeleteAddress, 0);
                DeleteAccount(queuedDeleteAddress);
            }

            // Before the byzantium update, changes used to be commited after every transaction.
            if (Configuration.Version < EthereumRelease.Byzantium)
            {
                CommitChanges();
            }



            // Create a receipt and add it to our receipt list
            Receipt transactionReceipt = null;
            if (Configuration.Version >= EthereumRelease.Byzantium)
            {
                // After Byzantium, we simply return the success result in the state root field.
                byte[] successResult = executionResult.Succeeded ? new byte[] { 1 } : Array.Empty<byte>();
                transactionReceipt = new Receipt(successResult, BlockGasUsed, TransactionLogs);
            }
            else
            {
                transactionReceipt = new Receipt(Trie.GetRootNodeHash(), BlockGasUsed, TransactionLogs);
            }

            // Record our result status
            if (!executionResult.Succeeded)
            {
                Configuration.DebugConfiguration?.RecordException(new Exception("Transaction returned a failed result status."), false, true);
            }

            // Add our receipt to our receipt list
            TransactionReceipts.Add(transactionReceipt);

            // Set our state's bloom from this receipt's bloom
            BigInteger oldBloom = Bloom;
            Bloom |= transactionReceipt.Bloom;

            // Clear our logs
            TransactionLogs.Clear();
        }
        #endregion

        #region State Variable Modifying Functions

        /// <summary>
        /// Obtains the nonce for an account at a given address.
        /// </summary>
        /// <param name="address">The address of the account to grab the nonce of.</param>
        /// <returns>Returns the nonce from the account at the given address.</returns>
        public BigInteger GetNonce(Address address)
        {
            // Obtain our account and it's nonce
            Account account = GetAccount(address);
            return account.Nonce;
        }

        /// <summary>
        /// Sets the nonce for an account at a given address.
        /// </summary>
        /// <param name="address">The address of the account to set the nonce of.</param>
        /// <param name="nonce">The nonce to set for the account at the given address.</param>
        public void SetNonce(Address address, BigInteger nonce)
        {
            // Obtain our account
            Account account = GetAccount(address);

            // Set our nonce
            account.Nonce = nonce;

            // Set our account as dirty
            SetAccountDirty(address);
        }

        /// <summary>
        /// Increments the nonce for an account at a given address.
        /// </summary>
        /// <param name="address">The address of the account to increment the nonce at.</param>
        public void IncrementNonce(Address address)
        {
            // Obtain our account
            Account account = GetAccount(address);

            // Increment our nonce
            account.Nonce++;

            // Set our account as dirty
            SetAccountDirty(address);
        }

        /// <summary>
        /// Verifies an account existed at the start of the current execution and wasn't deleted during this execution. (Past-Spurious Dragon we simply check if an account is blank or not).
        /// </summary>
        /// <param name="address">The address of the account to check existence of.</param>
        /// <returns>Returns a boolean indicating the state contains the account.</returns>
        public bool ContainsAccount(Address address)
        {
            // Past spurious dragon we check if an account is blank or not to see if it exists.
            if (Configuration.Version >= EthereumRelease.SpuriousDragon)
            {
                // If it's a non blank account, it exists.
                return !GetAccount(address).IsBlank;
            }

            // Otherwise we care about if the account was deleted since the last commit, not if its blank.
            Account account = GetAccount(address);

            // If the account is dirty, then we base it off whether the account was deleted or not
            if (account.IsDirty)
            {
                return !account.IsDeleted;
            }

            // Otherwise we just verify the account isn't new as of the current execution.
            return !account.IsNew;
        }

        /// <summary>
        /// Obtains the account with the given address, or creates one if the mentioned one does not exist.
        /// </summary>
        /// <param name="address">The address of the account to obtain or create.</param>
        /// <returns>Returns the account at the provided address or creates one if it does not exist.</returns>
        public Account GetAccount(Address address)
        {
            // If our address is not in the cache, obtain it (or create if it doesn't exist) and put it into the cache.
            if (!CachedAccounts.TryGetValue(address, out var account))
            {
                // Obtain our RLP data for the account from the trie.
                byte[] rlpData = Trie.Get(address.ToByteArray());
                if (rlpData == null)
                {
                    // No existing account at this point so we create one.
                    account = new Account(Configuration);
                }
                else
                {
                    // We decode the existing account accordingly.
                    account = new Account(Configuration, rlpData);
                }

                // Add our account to our cached accounts
                CachedAccounts[address] = account;
            }

            // Return our cached account.
            return account;
        }

        /// <summary>
        /// Queues an account at a given address to be deleted after the transaction has finished processing.
        /// </summary>
        /// <param name="address">The address of the account to be deleted after the transaction has finished processing.</param>
        public void QueueDeleteAccount(Address address)
        {
            // Add the address to our deleted accounts list
            AccountDeleteQueue.Add(address);
        }

        /// <summary>
        /// Deletes an account immediately by marking it as resetting all of it's properties and setting it as deleted.
        /// </summary>
        /// <param name="address">The address of the account to delete immediately.</param>
        private void DeleteAccount(Address address)
        {
            // Set our balance and nonce to zero
            SetBalance(address, 0);
            SetNonce(address, 0);

            // Set our code segment to an empty array.
            SetCodeSegment(address, Array.Empty<byte>());

            // Reset the account's storage data
            ResetStorageData(address);

            // Set the account as deleted.
            Account account = GetAccount(address);
            account.IsDeleted = true;

            // Set the account's dirty status.
            SetAccountDirty(address, false);
        }

        /// <summary>
        /// Sets an account as dirty/modified so it is known changes should be committed.
        /// </summary>
        /// <param name="address">The account to mark as dirty/modified.</param>
        private void SetAccountDirty(Address address, bool isDirty = true)
        {
            // Set our account's dirty status
            CachedAccounts[address].IsDirty = isDirty;
        }

        /// <summary>
        /// Obtains the balance for an account at a given address.
        /// </summary>
        /// <param name="address">The address of the account we wish to obtain the balance for.</param>
        /// <returns>Returns the balance of the account at the given address.</returns>
        public BigInteger GetBalance(Address address)
        {
            // Obtain our account and it's balance
            Account account = GetAccount(address);
            return account.Balance;
        }

        /// <summary>
        /// Sets the balance of an account at a given address.
        /// </summary>
        /// <param name="address">The address of the account we wish to set the balance for.</param>
        /// <param name="balance">The balance we wish to set for the account at the given address.</param>
        public void SetBalance(Address address, BigInteger balance)
        {
            // Obtain our account
            Account account = GetAccount(address);

            // Set our new balance.
            account.Balance = balance;

            // Set our account as dirty
            SetAccountDirty(address);
        }

        /// <summary>
        /// Modifies the balance of an account at a given address with the given delta/change value.
        /// </summary>
        /// <param name="address">The address of the account to add delta to the balance of.</param>
        /// <param name="delta">The amount to change the balance of the account at the given address.</param>
        public void ModifyBalanceDelta(Address address, BigInteger delta)
        {
            // If our change in balance is zero, we don't need to do anything.
            if (delta == 0)
            {
                return;
            }

            // Obtain our account
            Account account = GetAccount(address);

            // Modify our balance by the given delta.
            account.Balance += delta;

            // Set our account as dirty
            SetAccountDirty(address);
        }

        /// <summary>
        /// Tranfers a given balance amount from one account to another. 
        /// </summary>
        /// <param name="from">The address of the account to take the amount from.</param>
        /// <param name="to">The address of the account to give the amount to.</param>
        /// <param name="amount">The amount to take from one account and give to the other.</param>
        /// <returns>Returns true if the transfer was successful, false otherwise (such as if the balance failed).</returns>
        public bool TransferBalance(Address from, Address to, BigInteger amount)
        {
            // Assert our amount isn't somehow negative.
            if (amount < 0)
            {
                throw new ArgumentException("Transfer amount cannot be a negative integer.");
            }

            // If our amount to transfer is zero, we don't need to do anything.
            else if (amount == 0)
            {
                return true;
            }

            // Verify our address sending has the balance amount they want to transfer.
            if (GetBalance(from) < amount)
            {
                return false;
            }

            // Transfer the amount
            ModifyBalanceDelta(from, -amount);
            ModifyBalanceDelta(to, amount);

            // Indicate our operation succeeded.
            return true;
        }

        /// <summary>
        /// Obtains the code segment from an account at the provided address.
        /// </summary>
        /// <param name="address">The address of the account to obtain the code segment for.</param>
        /// <returns>Returns the code segment of the account at the provided address.</returns>
        public byte[] GetCodeSegment(Address address)
        {
            // Obtain our account
            Account account = GetAccount(address);

            // Using the code hash as a key, we obtain the code from our database.
            if (Configuration.Database.TryGet(account.CodeHash, out var val))
            {
                return val;
            }
            else
            {
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Sets the code segment on an account at the provided address.
        /// </summary>
        /// <param name="address">The address of the account to set the code segment for.</param>
        /// <param name="code">The code to set for the account's code segment.</param>
        public void SetCodeSegment(Address address, byte[] code)
        {
            // Obtain our account
            Account account = GetAccount(address);

            // Obtain our new code hash
            byte[] newCodeHash = KeccakHash.ComputeHashBytes(code);

            // Set our code in our database.
            Configuration.Database.Set(newCodeHash, code);

            // Set our code hash
            account.CodeHash = newCodeHash;

            // Set our account as dirty
            SetAccountDirty(address);
        }

        /// <summary>
        /// Obtains the storage value for the provided key from the account at the provided address.
        /// </summary>
        /// <param name="address">The address of the account to obtain storage data from.</param>
        /// <param name="key">The storage key to obtain the value for from the account.</param>
        /// <returns>Returns the value corresponding to the key in the storage of the account at the provided address.</returns>
        public BigInteger GetStorageData(Address address, BigInteger key)
        {
            // Obtain our account
            Account account = GetAccount(address);

            // Obtain our storage data.
            byte[] keyData = BigIntegerConverter.GetBytes(key, EVMDefinitions.WORD_SIZE);
            byte[] storageData = account.ReadStorage(keyData);
            return BigIntegerConverter.GetBigInteger(storageData);
        }

        /// <summary>
        /// Sets the storage value for the provided key for the account at the provided address.
        /// </summary>
        /// <param name="address">The address of the account to set storage data for.</param>
        /// <param name="key">The storage key for which we wish to set the value for in the account.</param>
        /// <param name="value">The storage value to set for the provided key in the account.</param>
        public void SetStorageData(Address address, BigInteger key, BigInteger value)
        {
            // Obtain our account
            Account account = GetAccount(address);

            // Obtain our old storage data and new storage data.
            byte[] keyData = BigIntegerConverter.GetBytes(key, EVMDefinitions.WORD_SIZE);
            byte[] newStorageData = BigIntegerConverter.GetBytes(value, EVMDefinitions.WORD_SIZE);

            // If the value is zero, set the new storage data to null
            if (value == 0)
            {
                newStorageData = null;
            }

            // Set the new storage data.
            account.WriteStorage(keyData, newStorageData);

            // Set the account as modified
            SetAccountDirty(address);
        }

        /// <summary>
        /// Resets the storage data for an account at the provided address.
        /// </summary>
        /// <param name="address">The address of the account to reset storage for.</param>
        public void ResetStorageData(Address address)
        {
            // Obtain the account at the given address
            Account account = GetAccount(address);

            // Reset the account's storage cache, backing up the old one.
            account.StorageCache = new Dictionary<Memory<byte>, byte[]>(new MemoryComparer<byte>());

            // Set our storage root node as a black node.
            account.StorageTrie.LoadRootNodeFromHash(Trie.BLANK_NODE_HASH);
        }

        /// <summary>
        /// Adds to the amount of gas which should be refunded after execution for the current transaction.
        /// </summary>
        /// <param name="value">The amount to add to the gas to refund. Must be positive.</param>
        public void AddGasRefund(BigInteger value)
        {
            // If the refund value is negative, throw an exception.
            if (value < 0)
            {
                throw new ArgumentException("Cannot add a negative refund amount.");
            }

            // If our amount to add is zero, we don't need to do anything.
            else if (value == 0)
            {
                return;
            }

            // Add our value to refund
            TransactionRefunds += value;
        }

        /// <summary>
        /// Adds a log to our state's log list.
        /// </summary>
        /// <param name="log">The log to add to our state's log list.</param>
        public void Log(Log log)
        {
            // Add to our log
            TransactionLogs.Add(log);
        }
        #endregion

        #region State Snapshot/Revert/Commit
        /// <summary>
        /// Obtains a snapshot/saved state of this state instance.
        /// </summary>
        /// <returns>Returns a snapshot/saved state of this state instance.</returns>
        public StateSnapshot Snapshot()
        {
            // Return a state snapshot of the state currently
            return new StateSnapshot(this);
        }

        /// <summary>
        /// Reverts this state instance to that of a provided snapshot/saved state.
        /// </summary>
        /// <param name="savedState">The snapshot/saved state to revert to. If null, then the change journal is used to rollback to the state of the last commit.</param>
        public void Revert(StateSnapshot savedState)
        {
            // Verify we were given a state.
            if (savedState == null)
            {
                throw new ArgumentNullException("Failed to revert state because provided state snapshot was null.");
            }

            // Apply our state snapshot to this state
            savedState.Apply(this);
        }

        /// <summary>
        /// Commits all uncommitted changes to the state trie.
        /// </summary>
        public void CommitChanges()
        {
            // Loop for every cached account with possible changes to it.
            foreach (Address address in CachedAccounts.Keys)
            {
                // Grab the account for this address
                Account account = CachedAccounts[address];

                // If the account was modified or deleted, we'll want to commit changes
                if (account.IsDirty || account.IsDeleted)
                {
                    // Commit any storage data changes on this account.
                    account.CommitStorageChanges();

                    // Any accounts added we'll want to set in our trie, any deleted we'll want to remove.
                    if (ContainsAccount(address))
                    {
                        Trie.Set(address.ToByteArray(), RLP.Encode(account.Serialize()));
                    }
                    else
                    {
                        Trie.Remove(address.ToByteArray());
                    }
                }
            }

            // We'll clear our cached accounts and journal 
            CachedAccounts.Clear();
        }

        /// <summary>
        /// Clones the current state by building a new instance from a snapshot of this one.
        /// </summary>
        /// <returns>Returns a clone of the current state object.</returns>
        public object Clone()
        {
            // Clone our state by snapshotting it and restoring it.
            return Snapshot().ToState();
        }
        #endregion

        #endregion
    }
}
