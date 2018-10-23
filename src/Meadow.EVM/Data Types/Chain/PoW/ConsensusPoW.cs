using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Block;
using Meadow.EVM.Data_Types.State;
using Meadow.EVM.Data_Types.Trees.Comparer;
using Meadow.EVM.Exceptions;

namespace Meadow.EVM.Data_Types.Chain.PoW
{
    /// <summary>
    /// Ethereum proof-of-work consensus mechamism state transition and helper functions implementation.
    /// </summary>
    // Code coverage disabled while tests are disabled for performance reasons.
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ConsensusPoW : ConsensusBase
    {
        #region Functions
        /// <summary>
        /// Given a block, enters the initialization state for handling a block.
        /// </summary>
        /// <param name="state">The state to set.</param>
        /// <param name="block">The block which will be processed.</param>
        public override void Initialize(State.State state, Block.Block block)
        {
            // We enter our initialization state
            state.BlockGasUsed = 0;
            state.Bloom = 0;
            state.TransactionReceipts.Clear();

            // Set our current block
            state.UpdateCurrentBlock(block);

            // TODO: Handle DAO fork transfer
        }

        /// <summary>
        /// Checks that the proof for this consensus mechanism is valid. (Checks proof of work, proof of stake, etc).
        /// </summary>
        /// <param name="state">The current state we have while checking proof.</param>
        /// <param name="blockHeader">The block which we are checking proof for.</param>
        /// <returns>Returns true if proof is valid. Returns false if proof is invalid.</returns>
        public override bool CheckProof(State.State state, BlockHeader blockHeader)
        {
            // Check our proof of work.
            return state.Configuration.IgnoreEthashVerification || MiningPoW.CheckProof(blockHeader.BlockNumber, blockHeader.GetMiningHash(), blockHeader.MixHash, blockHeader.Nonce, blockHeader.Difficulty);
        }

        /// <summary>
        /// Verifies all uncles in a given block to be processed.
        /// </summary>
        /// <param name="state">The state which accompanied the block to verify the uncles of.</param>
        /// <param name="block">The block to verify the uncles of.</param>
        /// <returns>Returns true if verification succeeded, returns false or throws an exception otherwise.</returns>
        public override bool VerifyUncles(State.State state, Block.Block block)
        {
            // Verify our uncles hash matches
            if (!block.Header.UnclesHash.ValuesEqual(block.CalculateUnclesHash()))
            {
                throw new BlockException("Block validation failed due to a hash mismatch when validating uncles.");
            }

            // Verify our uncle hash count did not exceed the maximum uncle count
            if (block.Uncles.Length > state.Configuration.MaxUncleDepth)
            {
                throw new BlockException("Block validation failed because the amount of uncles attached exceeded the maximum uncle count.");
            }

            // By definition, any uncle should be lower block number than the given block, since its only included as an uncle after it fails to be included at its desired block number.
            foreach (BlockHeader uncle in block.Uncles)
            {
                if (uncle.BlockNumber >= block.Header.BlockNumber)
                {
                    throw new BlockException("Block validation failed because an uncle's block number was not less than the block number it was included in.");
                }
            }

            // Obtain our list of ancestors.
            BlockHeader[] ancestors = new BlockHeader[Math.Min(state.Configuration.MaxUncleDepth + 1, state.PreviousHeaders.Count) + 1];
            ancestors[0] = block.Header;
            for (int i = 1; i < ancestors.Length; i++)
            {
                ancestors[i] = state.PreviousHeaders[i - 1];
            }

            // Create our valid ancestor list, the uncle header could've been
            Dictionary<byte[], BlockHeader> validUncleParents = new Dictionary<byte[], BlockHeader>();
            for (int i = 2; i < ancestors.Length; i++)
            {
                validUncleParents[ancestors[i].GetHash()] = ancestors[i];
            }

            // Uncles cannot be ancestors, and also can only be included once every max uncle depth.
            Dictionary<byte[], bool> invalidUncleList = new Dictionary<byte[], bool>(new ArrayComparer<byte[]>());
            foreach (var recentUncleHashes in state.RecentUncleHashes)
            {
                // Verify this uncle list comes from a block number before the current block.
                if (state.CurrentBlock.Header.BlockNumber <= recentUncleHashes.Key)
                {
                    continue;
                }

                // Verify this uncle list comes at a block number not past our max uncle depth from our current block number.
                if (recentUncleHashes.Key < state.CurrentBlock.Header.BlockNumber - state.Configuration.MaxUncleDepth)
                {
                    continue;
                }

                // Add our uncle hash to our list.
                foreach (byte[] uncleHash in recentUncleHashes.Value)
                {
                    invalidUncleList[uncleHash] = true;
                }
            }

            // Loop for all this current blocks uncles
            foreach (BlockHeader uncle in block.Uncles)
            {
                // Verify uncle's parent is part of our valid list
                if (!validUncleParents.TryGetValue(uncle.PreviousHash, out var parent))
                {
                    throw new BlockException("Block validation failed because uncles previous hash referenced a block that was not deemed a valid uncle parent.");
                }

                // Verify our difficulty
                BigInteger calculatedDifficulty = Block.Block.CalculateDifficulty(parent, uncle.Timestamp, state.Configuration);
                if (uncle.Difficulty != calculatedDifficulty)
                {
                    throw new BlockException($"Block validation failed because uncle had a difficulty mismatch. Expected = {calculatedDifficulty}, Actual = {uncle.Difficulty}");
                }

                // Verify block number
                if (uncle.BlockNumber != parent.BlockNumber + 1)
                {
                    throw new BlockException("Block validation failed because uncle's block number was not sequentially following its parent.");
                }

                // Verify timestamp
                if (uncle.Timestamp < parent.Timestamp)
                {
                    throw new BlockException($"Block validation failed because uncle's timestamp was less than its parent. Uncle = {uncle.Timestamp}, Parent={parent.Timestamp}");
                }

                // Verify our uncle isn't in our invalid list
                byte[] uncleHash = uncle.GetHash();
                if (invalidUncleList.ContainsKey(uncleHash))
                {
                    throw new BlockException("Block validation failed because uncle was also a direct ancestor or didn't meet depth requirements.");
                }

                // Verify our uncle didn't use more gas than the limit
                if (uncle.GasUsed > uncle.GasLimit)
                {
                    throw new BlockException("Block validation failed because uncle gas used exceeded the uncle gas limit.");
                }

                // Check the proof for our uncle
                if (!CheckProof(state, uncle))
                {
                    throw new BlockException("Block validation failed because uncle has a proof of work check failure!");
                }

                // Add this uncle to in invalid uncle list, as there should be no duplicates going forward.
                invalidUncleList[uncleHash] = true;
            }

            // Return our boolean indicating we succeeded.
            return true;
        }

        /// <summary>
        /// Given a chain and state, obtains all possible uncle candidates for the next block.
        /// </summary>
        /// <param name="chain">The chain we are currently operating on.</param>
        /// <param name="state">The state we are currently operating on.</param>
        /// <returns>Returns a list of possible uncle candidates for the next block.</returns>
        public override List<BlockHeader> GetUncleCandidates(ChainPoW chain, State.State state)
        {
            // Create our uncles list.
            List<BlockHeader> uncles = new List<BlockHeader>();

            // Uncles cannot be ancestors, and also can only be included once every max uncle depth.
            Dictionary<Memory<byte>, bool> invalidUncleList = new Dictionary<Memory<byte>, bool>(new MemoryComparer<byte>());
            foreach (byte[][] uncleHashes in state.RecentUncleHashes.Values)
            {
                foreach (byte[] uncleHash in uncleHashes)
                {
                    invalidUncleList[uncleHash] = true;
                }
            }

            // We'll want to make sure all previous headers can't be uncles
            int previousHeaderCount = Math.Min(state.Configuration.MaxUncleDepth, state.PreviousHeaders.Count);
            for (int i = 0; i < previousHeaderCount; i++)
            {
                invalidUncleList[state.PreviousHeaders[i].GetHash()] = true;
            }

            // Now we can populate our uncles
            for (int i = 0; i < previousHeaderCount; i++)
            {
                // Get all child hashes for the previous header
                byte[][] childHashes = chain.GetChildHashes(state.PreviousHeaders[i].GetHash());
                foreach (byte[] childHash in childHashes)
                {
                    // Verify it's a valid uncle, then add it.
                    if (!invalidUncleList.ContainsKey(childHash))
                    {
                        uncles.Add(chain.GetBlock(childHash).Header);
                    }

                    // If we reached our desired uncle count, we stop
                    if (uncles.Count == state.Configuration.MaxUncles)
                    {
                        return uncles;
                    }
                }
            }

            // Return what uncles we could obtain.
            return uncles;
        }

        /// <summary>
        /// Given a block, enters the finalization state for handling a block.
        /// </summary>
        /// <param name="state">The state to set.</param>
        /// <param name="block">The block which will be processed.</param>
        public override void Finalize(State.State state, Block.Block block)
        {
            // Obtain the rewards accordingly
            BigInteger blockReward = 0;
            BigInteger nephewReward = 0;
            if (state.Configuration.Version >= Configuration.EthereumRelease.Byzantium)
            {
                blockReward = state.Configuration.BlockRewardByzantium;
                nephewReward = state.Configuration.NephewRewardByzantium;
            }
            else
            {
                blockReward = state.Configuration.BlockReward;
                nephewReward = state.Configuration.NephewReward;
            }

            // Calculate our total reward, which is our reward for the immediate block, plus rewards for each uncle processed.
            BigInteger totalReward = blockReward + (nephewReward * block.Uncles.Length);

            // Award the money to the coinbase
            state.ModifyBalanceDelta(state.CurrentBlock.Header.Coinbase, totalReward);

            // Next, we'll want to factor in our depth penalty factor for our uncles, depending on their distance from the current block.
            foreach (BlockHeader uncle in block.Uncles)
            {
                // We apply our factor to our uncle distance from the current block to obtain a multiplier for reward which takes into account penalty, which we multiply the reward by.
                BigInteger uncleReward = blockReward * (state.Configuration.UncleDepthPenaltyFactor + (uncle.BlockNumber - state.CurrentBlock.Header.BlockNumber)) / state.Configuration.UncleDepthPenaltyFactor;

                // Award our reward to the uncle's coinbase.
                state.ModifyBalanceDelta(uncle.Coinbase, uncleReward);
            }

            // We'll want to remove any recent uncles past our max after finalizing this block.
            BigInteger lastUncleBlockNumber = state.CurrentBlock.Header.BlockNumber - state.Configuration.MaxUncleDepth;

            // Check if this uncle is in our recent uncle list
            state.RecentUncleHashes.Remove(lastUncleBlockNumber);
        }
        #endregion
    }
}
