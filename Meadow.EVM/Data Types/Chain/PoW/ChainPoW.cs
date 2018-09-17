using Meadow.Core.Cryptography;
using Meadow.Core.RlpEncoding;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Block;
using Meadow.EVM.Data_Types.Trees;
using Meadow.EVM.Data_Types.Trees.Comparer;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Data_Types.Chain.PoW
{
    public class ChainPoW
    {
        #region Constants
        private const string DB_PREFIX_CHILD = "blkc";
        private const string DB_PREFIX_BLOCKNUMBER = "blkn";
        private const string DB_PREFIX_SCORE = "blks";
        private const string DB_PREFIX_TRANSACTION_INDEX = "txin";
        private const string DB_HEAD_HASH = "head";
        #endregion

        #region Properties
        /// <summary>
        /// Defines the configuration for the Ethereum implementation, including fork/version implementation, etc.
        /// </summary>
        public Configuration.Configuration Configuration { get; private set; }
        /// <summary>
        /// The Ethereum world state
        /// </summary>
        public State.State State { get; private set; }
        /// <summary>
        /// The hash of the leading block in this chain (the most recent processed and added one).
        /// </summary>
        public byte[] HeadBlockHash { get; private set; }

        /// <summary>
        /// Priority queue for our queuing blocks with a future timestamp to process.
        /// </summary>
        public Heap<Block.Block> QueuedBlocks { get; private set; }
        /// <summary>
        /// A list of blocks which have no parent yet but are ready to added when the parent appears.
        /// </summary>
        public Dictionary<Memory<byte>, List<Block.Block>> OrphanBlocks { get; private set; }
        #endregion

        #region Constructor
        public ChainPoW(Configuration.Configuration configuration = null)
        {
            // Initialize our queued blocks priority queue.
            QueuedBlocks = new Heap<Block.Block>(HeapOrder.Min, new Heap<Block.Block>.CompareFunction(CompareBlockQueue));
            OrphanBlocks = new Dictionary<Memory<byte>, List<Block.Block>>(new MemoryComparer<byte>());

            // If the configuration is null, we create a new one.
            if (configuration == null)
            {
                configuration = new Configuration.Configuration();
            }

            // Set our configuration
            Configuration = configuration;
            Configuration.Consensus = new ConsensusPoW();

            // Check if our head is in the database
            if (Configuration.Database.Contains(DB_HEAD_HASH))
            {
                // Get obtain our head hash
                if (!Configuration.Database.TryGet(DB_HEAD_HASH, out var headBlockHash))
                {
                    throw new Exception($"Database head hash is missing from database");
                }

                HeadBlockHash = headBlockHash;

                // We obtain the post-block-processing state.
                State = GetPostBlockState(HeadBlockHash);
            }
            else
            {
                // Set up a new genesis state from our genesis state snapshot.
                State = Configuration.GenesisStateSnapshot.ToState();

                // Update the current block we're executing (or have executed)
                State.UpdateCurrentBlock(Configuration.GenesisBlock);
                HeadBlockHash = State.CurrentBlock.Header.GetHash();
                State.PreviousHeaders.Add(Configuration.GenesisBlock.Header);

                // Set our genesis block in our chain database
                AddGenesisBlockToDatabase();
            }
        }
        #endregion

        #region Functions
        // Main Functions
        /// <summary>
        /// The default block comparing method for any queued blocks.
        /// </summary>
        /// <param name="first">The first block to compare for ordering in the queue.</param>
        /// <param name="second">The second block to compare for ordering in the queue.</param>
        /// <returns>Returns less than zero if the first blocks timestamp comes first, zero if they are equal, and greater than zero if the first blocks timestamp comes second.</returns>
        private int CompareBlockQueue(Block.Block first, Block.Block second)
        {
            // Blocks will be compared by timestamp.
            return first.Header.Timestamp.CompareTo(second.Header.Timestamp);
        }

        /// <summary>
        /// Processes any queued blocks which were set to be processed for a future time when added.
        /// </summary>
        public void ProcessQueuedBlocks()
        {
            // If we have queued items, we pop and process all queued items which are ready.
            while (QueuedBlocks.Count > 0 && QueuedBlocks.Peek().Header.Timestamp <= Configuration.CurrentTimestamp)
            {
                AddBlock(QueuedBlocks.Pop());
            }
        }

        /// <summary>
        /// Adds a block to the chain, or queues it to be added later if the block's timestamp has not passed yet.
        /// </summary>
        /// <param name="block">The block to add or queue to be added to the chain.</param>
        /// <param name="newState">An optional parameter which implies the new state after executing the block.</param>
        /// <returns>Returns true if the block was processed immediately, otherwise returns false if it was queued to be added later or was seen as problematic and will not be added at all.</returns>
        public bool AddBlock(Block.Block block, State.State newState = null)
        {
            // If it isn't time for the block to be added yet, we instead add it to our queue.
            if (block.Header.Timestamp > Configuration.CurrentTimestamp)
            {
                QueuedBlocks.Push(block);
                return false;
            }

            // Check if the block is being added to the head of the chain
            if (block.Header.PreviousHash.ValuesEqual(HeadBlockHash))
            {
                // Apply block
                if (newState != null)
                {
                    State = newState;
                }
                else
                {
                    try
                    {
                        State.ApplyBlock(block);
                    }
                    catch (Exception exception)
                    {
                        // Record the exception that occured for this block.
                        Configuration.DebugConfiguration.RecordException(exception, false);
                        return false;
                    }
                }

                // Set block hash for block number
                SetBlockHashForBlockNumber(block.Header.BlockNumber, block.Header.GetHash());

                // Get block score so it is cached in the database
                GetScore(block);

                // Set the head to this block
                HeadBlockHash = block.Header.GetHash();

                // For every transaction, set the block number and index
                for (int i = 0; i < block.Transactions.Length; i++)
                {
                    SetTransactionPosition(block.Transactions[i].GetHash(), block.Header.BlockNumber, i);
                }
            }
            else if (ContainsBlock(block.Header.PreviousHash))
            {
                // If the previous hash is in the chain, but it's not the head, we process it to see if it will get a better score
                Data_Types.State.State state = GetPostBlockState(block.Header.PreviousHash);

                // Apply block
                try
                {
                    state.ApplyBlock(block);
                }
                catch (Exception exception)
                {
                    // Record the exception that occured for this block.
                    Configuration.DebugConfiguration.RecordException(exception, false);
                    return false;
                }

                // Obtain our block score
                BigInteger newScore = GetScore(block);
                BigInteger currentScore = GetScore(GetHeadBlock());

                // If our new score is better than our head score, we replace the head.
                if (newScore > currentScore)
                {
                    // Set the head as the new block head
                    HeadBlockHash = block.Header.GetHash();
                    State = state;

                    // We find a common ancestor with our existing head while obtaining our new chain.
                    Block.Block currentBlock = block;
                    Dictionary<BigInteger, Block.Block> newChain = new Dictionary<BigInteger, Block.Block>();
                    while (currentBlock != null && currentBlock.Header.BlockNumber >= Configuration.GenesisBlock.Header.BlockNumber)
                    {
                        // Add our new block to the new chain
                        newChain[currentBlock.Header.BlockNumber] = currentBlock;

                        // Obtain the original at this block number
                        byte[] originalBlockHash = GetBlockHashFromBlockNumber(currentBlock.Header.BlockNumber);

                        // If this is a common ancestor or it doesn't exist in our database, we stop
                        if (originalBlockHash == null || originalBlockHash.ValuesEqual(currentBlock.Header.GetHash()))
                        {
                            break;
                        }

                        currentBlock = GetParentBlock(currentBlock);
                    }

                    // Current block by now is a common ancestor or it's null (presumably if we looked past genesis without finding a common ancestor, which shouldn't happen unless our two heads have differing genesis blocks).

                    // We loop from the common ancestor forward while we have old blocks to remove, and new blocks to add (replacing part of our chain).
                    for (BigInteger i = currentBlock.Header.BlockNumber; true; i++)
                    {
                        // Obtain our block hash for this index
                        byte[] originalBlockHash = GetBlockHashFromBlockNumber(i);

                        // If this old chain exists at this index.
                        bool originalChainExists = originalBlockHash != null;
                        bool newChainExists = newChain.ContainsKey(i);
                        if (originalChainExists)
                        {
                            // Obtain our original block.
                            Block.Block originalBlock = GetBlock(originalBlockHash);

                            // Remove the block hash for this index.
                            RemoveBlockHashForBlockNumber(i);

                            // Remove all transaction index lookup items.
                            foreach (Transactions.Transaction transaction in originalBlock.Transactions)
                            {
                                RemoveTransactionPosition(transaction.GetHash());
                            }
                        }

                        // If our new chain exists at this index.
                        if (newChainExists)
                        {
                            // Obtain our new block.
                            Block.Block newBlock = newChain[i];

                            // Set our block hash for this index
                            SetBlockHashForBlockNumber(i, newBlock.Header.GetHash());

                            // Set all transaction indexes for each transaction
                            for (int transactionIndex = 0; transactionIndex < newBlock.Transactions.Length; transactionIndex++)
                            {
                                SetTransactionPosition(newBlock.Transactions[transactionIndex].GetHash(), newBlock.Header.BlockNumber, transactionIndex);
                            }
                        }

                        // If neither chain exists here, we have no work to be doing
                        if (!newChainExists && !originalChainExists)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                // Block has no parent yet, we make sure we have an orphan list set up to queue this block for later.
                if (!OrphanBlocks.TryGetValue(block.Header.PreviousHash, out var orphanList))
                {
                    orphanList = new List<Block.Block>();
                    OrphanBlocks[block.Header.PreviousHash] = orphanList;
                }

                // Add this block to it.
                orphanList.Add(block);

                return false;
            }

            // Add this block to the child lookup to allow us to look both directions on the chain.
            AddChild(block);

            // Set our block in our database.
            SetBlock(block);

            // Update our head hash since we finished applying the block
            Configuration.Database.Set(DB_HEAD_HASH, HeadBlockHash);

            // Check if there are any orphaned blocks waiting for this parent.
            byte[] blockHash = block.Header.GetHash();
            if (OrphanBlocks.TryGetValue(blockHash, out var orphanBlocks))
            {
                // Process all orphans now that their parent arrived.
                foreach (Block.Block orphanBlock in orphanBlocks)
                {
                    AddBlock(orphanBlock);
                }

                // Clear the list.
                OrphanBlocks.Remove(blockHash);
            }

            return true;
        }

        /// <summary>
        /// Obtains a State instance that represents the state after the block with the given hash was processed.
        /// </summary>
        /// <param name="blockHash">The hash of the block to obtain post-processed state of.</param>
        /// <returns>Returns a State instance that represents the state after the block with the given hash was processed.</returns>
        public State.State GetPostBlockState(byte[] blockHash)
        {
            // Obtain the block.
            Block.Block block = GetBlock(blockHash);
            if (block == null)
            {
                return null;
            }

            // Otherwise we obtain the state with the block's given state root hash and set the current block.
            State.State state = new State.State(Configuration, block.Header.StateRootHash);
            state.UpdateCurrentBlock(block);
            state.BlockGasUsed = block.Header.GasUsed;

            // We'll want to populate our previous block hashes for this state (and one for this block)
            Block.Block currentBlock = block;
            for (int i = 0; i < Configuration.PreviousHashDepth + 1; i++)
            {
                // Add our previous header
                state.PreviousHeaders.Add(currentBlock.Header);

                // If our index is less than our max uncle depth, we add our uncles
                if (i < Configuration.MaxUncleDepth)
                {
                    state.UpdateUncleHashes(currentBlock);
                }

                // Iterate to the previous block.
                currentBlock = GetParentBlock(currentBlock);
                if (currentBlock == null)
                {
                    break;
                }
            }

            return state;
        }

        /// <summary>
        /// Reverts this chain state using a state snapshot obtained from this chain's state earlier.
        /// </summary>
        /// <param name="snapshot"></param>
        public void Revert(State.StateSnapshot snapshot)
        {
            // Obtain our block number in our current state to know how much to roll back in our chain database.
            BigInteger laterBlockNumber = State.CurrentBlock.Header.BlockNumber;

            // Revert our state
            State.Revert(snapshot);

            // Obtain our block number after reverting so we know how much we need to roll back.
            BigInteger earlierBlockNumber = State.CurrentBlock.Header.BlockNumber;

            // Verify this is a revert operation that is occuring to a subset of this chain.
            if (earlierBlockNumber > laterBlockNumber)
            {
                throw new Exception("Reverting to a state that was not a subset of the current state is not yet supported.");
            }

            // Clear any children out (to avoid conflicts: normally snapshot/revert is only applied to state, not chain. Chain snapshot/revert is a test node feature only, and databases don't prune so old data post-revert can cause issues).
            // Note: We only delete children for all newer blocks and our most recent block, since reverting here, our head will have the same hash as originally, and may have children, and potential later blocks may too.
            // Because of this, we clear all children instances in our in-memory database so they don't get parsed as "uncles" waiting to be processed on the side. 
            for (BigInteger blockNum = laterBlockNumber; blockNum >= earlierBlockNumber; blockNum--)
            {
                // Obtain our block hash for this number
                byte[] blockHash = GetBlockHashFromBlockNumber(blockNum);

                // Remove the children.
                DeleteChildren(blockHash);

                // Remove the block number->block hash lookup and transaction position lookups
                // NOTE: We do this as long as we're not the genesis block, or the most recent block, since we still want information for those, just not anything after (children, etc).
                if (blockNum > 0 && blockNum > earlierBlockNumber)
                {
                    // Grab the block and remove transaction positions for it.
                    Block.Block block = GetBlock(blockHash);
                    foreach (var transaction in block.Transactions)
                    {
                        RemoveTransactionPosition(transaction.GetHash());
                    }

                    // Remove the block hash lookup for this block.
                    RemoveBlockHashForBlockNumber(blockNum);

                    // Remove the block data itself finally
                    RemoveBlock(blockHash);
                }
            }

            // Update our head block hash
            HeadBlockHash = GetBlockHashFromBlockNumber(earlierBlockNumber);
        }

        // Genesis Block
        /// <summary>
        /// Adds the genesis block from the current configuration to the chain's database.
        /// </summary>
        private void AddGenesisBlockToDatabase()
        {
            // Obtain the genesis block and it's hash
            Block.Block genesisBlock = Configuration.GenesisBlock;
            byte[] genesisBlockHash = genesisBlock.Header.GetHash();

            // Set the block number -> block hash lookup.
            SetBlockHashForBlockNumber(genesisBlock.Header.BlockNumber, genesisBlock.Header.GetHash());

            // Set the block hash -> block lookup
            SetBlock(genesisBlock);
        }

        // Block Contains/Get/Set
        /// <summary>
        /// Checks if a given block is known to this chain.
        /// </summary>
        /// <param name="block">The block to look up in our chain database.</param>
        /// <returns>Returns a boolean indicating if we have the provided block in our chain database.</returns>
        public bool ContainsBlock(Block.Block block)
        {
            // Check if we have this block by using our block hash
            return ContainsBlock(block.Header.GetHash());
        }

        /// <summary>
        /// Checks if a given block hash is known to the chain.
        /// </summary>
        /// <param name="blockHash">The block hash to look up in our chain database.</param>
        /// <returns>Returns a boolean indicating if we have a block with the given hash in our chain database.</returns>
        public bool ContainsBlock(byte[] blockHash)
        {
            // Indicate whether or not our database contains this block.
            return Configuration.Database.Contains(blockHash);
        }

        /// <summary>
        /// Obtains a block with the given block hash from the chain database.
        /// </summary>
        /// <param name="blockHash">The block hash to look up in our chain database.</param>
        /// <returns>Returns the block corresponding to the given block hash, or null if it could not be found.</returns>
        public Block.Block GetBlock(byte[] blockHash)
        {
            // If our block hash is null, we return null
            if (blockHash == null)
            {
                return null;
            }

            // Try to obtain our block.
            if (!Configuration.Database.TryGet(blockHash, out byte[] rlpBlock))
            {
                // If we fail to, return null.
                return null;
            }

            // Obtain our block
            return new Block.Block(RLP.Decode(rlpBlock));
        }

        /// <summary>
        /// Sets a given block in the chain database so it can be obtained with a block hash key.
        /// </summary>
        /// <param name="block">The block to set in the chain database.</param>
        public void SetBlock(Block.Block block)
        {
            // Obtain the block RLP data
            RLPItem rlpBlock = block.Serialize();
            byte[] rlpBlockData = RLP.Encode(rlpBlock);

            // Set our block data in our database.
            Configuration.Database.Set(block.Header.GetHash(), rlpBlockData);
        }

        /// <summary>
        /// Removes a block with the given block hash from the chain database.
        /// </summary>
        /// <param name="blockHash">The block hash to remove the key/value for in our chain database.</param>
        public void RemoveBlock(byte[] blockHash)
        {
            // If our block hash is null, we return null
            if (blockHash == null)
            {
                return;
            }

            // Remove our block data from our database.
            Configuration.Database.Remove(blockHash);
        }

        // Block Hierarchy
        /// <summary>
        /// Obtains the head (latest/leading) block in the chain.
        /// </summary>
        /// <returns>Returns the head block in the chain.</returns>
        public Block.Block GetHeadBlock()
        {
            // If our head hash is null, then we return the genesis block
            if (HeadBlockHash == null)
            {
                // Return the genesis block.
                return Configuration.GenesisBlock;
            }

            // Otherwise we obtain our head block by using it's hash, and decode it.
            if (!Configuration.Database.TryGet(HeadBlockHash, out var rlpBlockData))
            {
                throw new Exception($"Failed to get head block by hash {HeadBlockHash.ToHexString(hexPrefix: true)}");
            }

            RLPItem rlpBlock = RLP.Decode(rlpBlockData);
            return new Block.Block(rlpBlock);
        }

        /// <summary>
        /// Obtains the parent block of a provided block.
        /// </summary>
        /// <param name="block">The block to obtain the parent block of.</param>
        /// <returns>Retuns the parent block of the provided block.</returns>
        public Block.Block GetParentBlock(Block.Block block)
        {
            // If the block is the genesis block, there is no parent.
            if (block == null || block.Header.BlockNumber == Configuration.GenesisBlock.Header.BlockNumber)
            {
                return null;
            }

            // Otherwise we obtain the block by using the previous hash mentioned in the block header.
            return GetBlock(block.Header.PreviousHash);
        }

        /// <summary>
        /// Adds the child lookup entry to our database so a block can later check it's children quickly (since normally only parents are referenced in blocks, so this provides bi-directional lookup).
        /// </summary>
        /// <param name="child">The child block to add to our lookup and reference the parent of.</param>
        public void AddChild(Block.Block child)
        {
            // The underlying lookup for children: we use the parent hash in the lookup key, value is child hashes sequentially following eachother.
            byte[] childStructure = null;

            // Try to obtain the child structure.
            Configuration.Database.TryGet(DB_PREFIX_CHILD, child.Header.PreviousHash, out childStructure);

            // Obtain our child's hash
            byte[] childHash = child.Header.GetHash();

            // As a minimum, the children should consist of at least one hash.
            if (childStructure == null || childStructure.Length < KeccakHash.HASH_SIZE)
            {
                // We create a new structure.
                childStructure = childHash;
            }
            else
            {
                // We verify our child structure should be divisible by hash size
                if (childStructure.Length % KeccakHash.HASH_SIZE != 0)
                {
                    throw new Exception("Chain's block child lookup child structure should be divisible by Keccak256 digest size.");
                }

                // We verify our child hash isn't already in here.
                for (int i = 0; i < childStructure.Length; i += KeccakHash.HASH_SIZE)
                {
                    // Grab current the child hash.
                    byte[] existingChildHash = childStructure.Slice(i, i + KeccakHash.HASH_SIZE);

                    // If the child hash is already in our child structure, we stop.
                    if (childHash.ValuesEqual(existingChildHash))
                    {
                        return;
                    }
                }

                // We have a parent and child hash. We simply append our child block hash to the end.
                childStructure = childStructure.Concat(childHash);
            }

            // Set it in our database
            Configuration.Database.Set(DB_PREFIX_CHILD, child.Header.PreviousHash, childStructure);
        }

        /// <summary>
        /// Given the hash of a parent block, obtains the hashes of it's children.
        /// </summary>
        /// <param name="blockHash">The block header hash of the block we wish to find children for.</param>
        /// <returns>Returns an array of child block hashes.</returns>
        public byte[][] GetChildHashes(byte[] blockHash)
        {
            byte[] childStructure = null;
            Configuration.Database.TryGet(DB_PREFIX_CHILD, blockHash, out childStructure);

            // If our child structure doesn't exist or is malformed, we default to an empty child list
            if (childStructure == null || childStructure.Length < KeccakHash.HASH_SIZE)
            {
                return Array.Empty<byte[]>();
            }

            // We verify our child structure should be divisible by hash size
            if (childStructure.Length % KeccakHash.HASH_SIZE != 0)
            {
                throw new Exception("Chain's block child lookup child structure should be divisible by Keccak256 digest size.");
            }

            // Otherwise we split our child structure into it's respective hashes.
            byte[][] childHashes = new byte[childStructure.Length / KeccakHash.HASH_SIZE][];
            for (int i = 0; i < childHashes.Length; i++)
            {
                int start = i * KeccakHash.HASH_SIZE;
                int end = start + KeccakHash.HASH_SIZE;
                childHashes[i] = childStructure.Slice(start, end);
            }

            // Return our child hash array
            return childHashes;
        }

        /// <summary>
        /// Given the hash of a parent block, deletes the children hashes in our child lookup.
        /// </summary>
        /// <param name="blockHash">The block header hash of the block we wish to remove children for.</param>
        public void DeleteChildren(byte[] blockHash)
        {
            // Try to delete the item from our database.
            Configuration.Database.Remove(DB_PREFIX_CHILD, blockHash);
        }

        /// <summary>
        /// Given a block, obtains all child blocks.
        /// </summary>
        /// <param name="block">The block to obtain children for.</param>
        /// <returns>Returns all child blocks of the provided parent block.</returns>
        public Block.Block[] GetChildren(Block.Block block)
        {
            return GetChildren(block.Header);
        }

        /// <summary>
        /// Given a block header, obtains all child blocks.
        /// </summary>
        /// <param name="blockHeader">The block header to obtain children for.</param>
        /// <returns>Returns all child blocks for the parent block which the provided header belongs to.</returns>
        public Block.Block[] GetChildren(BlockHeader blockHeader)
        {
            return GetChildren(blockHeader.GetHash());
        }

        /// <summary>
        /// Given a block hash, obtains all child blocks.
        /// </summary>
        /// <param name="blockHash">The block hash for the parent block to obtain children for.</param>
        /// <returns>Returns all child blocks for the parent block which the provided block hash belongs to.</returns>
        public Block.Block[] GetChildren(byte[] blockHash)
        {
            // Get the child hash array
            byte[][] childHashes = GetChildHashes(blockHash);

            // Create a block array for each hash
            Block.Block[] children = new Block.Block[childHashes.Length];

            // For each child hash, we obtain the child
            for (int i = 0; i < childHashes.Length; i++)
            {
                children[i] = GetBlock(childHashes[i]);
            }

            // Return the list of children.
            return children;
        }

        // Block Number -> Block Hash
        /// <summary>
        /// Obtains a block hash corresponding to the provided block number.
        /// </summary>
        /// <param name="blockNumber">The block number of the block to obtain the hash for.</param>
        /// <returns>Returns the block hash for the block at the provided block number.</returns>
        public byte[] GetBlockHashFromBlockNumber(BigInteger blockNumber)
        {
            // Try to obtain our block hash from our database.
            if (Configuration.Database.TryGet(DB_PREFIX_BLOCKNUMBER, blockNumber.ToByteArray(), out var val))
            {
                return val;
            }

            return null;
        }

        /// <summary>
        /// Sets the block hash for a provided block number in our chain database.
        /// </summary>
        /// <param name="blockNumber">The block number for which we wish to set the block hash.</param>
        /// <param name="blockHash">The block hash which we wish to set for the provided block number.</param>
        public void SetBlockHashForBlockNumber(BigInteger blockNumber, byte[] blockHash)
        {
            // Set our block hash in our database.
            Configuration.Database.Set(DB_PREFIX_BLOCKNUMBER, blockNumber.ToByteArray(), blockHash);
        }

        /// <summary>
        /// Removed a block hash lookup for a certain block number.
        /// </summary>
        /// <param name="blockNumber">The block number to remove the block hash lookup for.</param>
        public void RemoveBlockHashForBlockNumber(BigInteger blockNumber)
        {
            // Remove our key
            Configuration.Database.Remove(DB_PREFIX_BLOCKNUMBER, blockNumber.ToByteArray());
        }

        // Block Score
        /// <summary>
        /// Obtains the score for this block. In PoW, this is the sum of all difficulty from the head, all the way back to the genesis block.
        /// </summary>
        /// <param name="block">The block for which we wish to obtain a score for.</param>
        /// <returns>Returns the score for the given block.</returns>
        public BigInteger GetScore(Block.Block block)
        {
            // If the block is null, we return 0
            if (block == null)
            {
                return 0;
            }

            // We'll be looking to calculate score, the sum of all difficulty for each block since genesis.
            BigInteger score = 0;
            Block.Block currentBlock = block;
            byte[] currentBlockHash = currentBlock.Header.GetHash(); // this calculates each time so we call it, so we store it instead of recalculating repetitively.

            // Loop from the provided block all the way back to the last known block score, tracking all block hashes and difficulties so we can store score while we compute it, looping forwards.
            List<(byte[] blockHash, BigInteger blockDifficulty)> unscoredBlocks = new List<(byte[] blockHash, BigInteger blockDifficulty)>();
            while (!Configuration.Database.Contains(DB_PREFIX_SCORE, currentBlockHash) && currentBlock != null)
            {
                // Add to our unscored blocks
                unscoredBlocks.Add((currentBlockHash, currentBlock.Header.Difficulty));

                // Move upwards
                currentBlock = GetParentBlock(currentBlock);
                if (currentBlock != null)
                {
                    currentBlockHash = currentBlock.Header.GetHash();
                }
            }

            // Reverse our list of unscored blocks so we can compute score, looping from our last scored block to the head.
            unscoredBlocks.Reverse();

            // Determine our base score, our last known score to calculate from.
            if (currentBlock == null)
            {
                // If current block is null, that means we looped back past genesis because it is unscored. We set base score to zero.
                score = 0;

                // Genesis score should be zero, so if it's unscored, we simply remove it, and thus it'll have zero weight in our summation.
                if (unscoredBlocks.Count > 0)
                {
                    unscoredBlocks.RemoveAt(0);
                }
            }
            else
            {
                // If the current block isn't null, we had a scored block, so we set our base score as the last scored block and start our summation from there.

                if (Configuration.Database.TryGet(DB_PREFIX_SCORE, currentBlockHash, out var val))
                {
                    score = new BigInteger(val);
                }
                else
                {
                    throw new Exception($"Failed to get block score from database for block: {currentBlockHash.ToHexString(hexPrefix: true)}");
                }
            }

            // Loop for each unscored block 
            foreach (var unscoredBlock in unscoredBlocks)
            {
                // Add to our score
                score += unscoredBlock.blockDifficulty;

                // Set our score in our database for this block.
                Configuration.Database.Set(DB_PREFIX_SCORE, unscoredBlock.blockHash, score.ToByteArray());
            }

            // Return the score
            return score;
        }

        // Transaction Index
        /// <summary>
        /// Obtains a transaction's block number and transaction index.
        /// </summary>
        /// <param name="transactionHash">The hash of the transaction to obtain indicies for.</param>
        /// <returns>Returns the block number and transaction index which hold this transaction.</returns>
        public (BigInteger blockNumber, BigInteger transactionIndex)? GetTransactionPosition(byte[] transactionHash)
        {
            // Try to obtain the rlp data from our database.
            if (!Configuration.Database.TryGet(DB_PREFIX_TRANSACTION_INDEX, transactionHash, out var rlpValue))
            {
                return null;
            }

            // Decode our transaction position information and return it.
            RLPList item = (RLPList)RLP.Decode(rlpValue);
            return (new BigInteger((byte[])item.Items[0]), new BigInteger((byte[])item.Items[1]));
        }

        /// <summary>
        /// Sets the transaction position in our database (block number and transaction index).
        /// </summary>
        /// <param name="transactionHash">The hash of the transaction for which to set the position.</param>
        /// <param name="blockNumber">The block number which holds the transaction.</param>
        /// <param name="transactionIndex">The index of the transaction in the provided block number.</param>
        public void SetTransactionPosition(byte[] transactionHash, BigInteger blockNumber, BigInteger transactionIndex)
        {
            // Declare our value
            byte[] value = RLP.Encode(new RLPList(blockNumber.ToByteArray(), transactionIndex.ToByteArray()));
            Configuration.Database.Set(DB_PREFIX_TRANSACTION_INDEX, transactionHash, value);
        }

        /// <summary>
        /// Removes the transaction position information from our database (block number and transaction index).
        /// </summary>
        /// <param name="transactionHash"></param>
        public void RemoveTransactionPosition(byte[] transactionHash)
        {
            // Remove the transaction index lookup from our database.
            Configuration.Database.Remove(DB_PREFIX_TRANSACTION_INDEX, transactionHash);
        }
        #endregion
    }
}
