using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Data_Types.Chain.PoW;
using Meadow.EVM.Data_Types.Transactions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

namespace Meadow.TestNode
{
    public class TestNodeChain
    {
        #region Fields
        private Semaphore _chainUpdateLock;
        #endregion

        #region Properties
        /// <summary>
        /// The operating chain and underlying components which we are adding to.
        /// </summary>
        public ChainPoW Chain { get; private set; }
        /// <summary>
        /// The transaction pool which holds all queue'd/pending transactions.
        /// </summary>
        public TransactionPool TransactionPool { get; private set; }

        /// <summary>
        /// Miner reward address.
        /// </summary>
        public Address Coinbase { get; private set; }
        /// <summary>
        /// The minimum gas price for a transaction to be considered being mined into a block.
        /// </summary>
        public BigInteger MinimumGasPrice { get; private set; }
        /// <summary>
        /// The extra data for every mined block.
        /// </summary>
        public byte[] ExtraData { get; private set; }
        /// <summary>
        /// Indicates whether we should trust the miner's resulting state and use that as our state, or whether we should re-apply the entire block to recalculate our own.
        /// </summary>
        public bool TrustMinerStates { get; private set; }
        #endregion

        #region Constructor
        public TestNodeChain(Configuration configuration)
        {
            // Initialize our threading lock
            _chainUpdateLock = new Semaphore(1, 1);

            // Create our test chain.
            Chain = new ChainPoW(configuration);

            // Set our mining properties.
            Coinbase = "0x7777777777777777777777777777777777777777";  // arbitrary miner reward address
            MinimumGasPrice = 0; //2 * BigInteger.Pow(10, 10); // the minimum amount someone can be willing to pay for a unit of gas.
            ExtraData = Chain.Configuration.GenesisBlock.Header.ExtraData;

            // Create our transaction pool
            TransactionPool = new TransactionPool();

            // Set our default trust miner value
            TrustMinerStates = false;
        }
        #endregion

        #region Functions
        private void HandleUpdates()
        {
            // Loop endlessly and look for updates (meant to be run from another thread).
            while (true)
            {
                MiningUpdate();
            }
        }

        public void MiningUpdate(bool forceMineBlock = false)
        {
            // Enter critical section
            _chainUpdateLock.WaitOne();

            // Process blocks that were queued because of future timestamps.
            Chain.ProcessQueuedBlocks();

            // Verify we have transactions in our pool.
            if (TransactionPool.Count > 0 || forceMineBlock)
            {
                // Determine if we're skipping miner coverage
                bool skipMinerCoverageAndTracing = !TrustMinerStates;

                // Backup our coverage enabled state and disable it if necessary.
                bool oldCoverageState = Chain.Configuration.CodeCoverage.Enabled;
                bool oldTracingState = Chain.Configuration.DebugConfiguration.IsTracing;
                if (skipMinerCoverageAndTracing)
                {
                    Chain.Configuration.CodeCoverage.Enabled = false;
                    Chain.Configuration.DebugConfiguration.IsTracing = false;
                }

                // Create a new head candidate.
                var candidateResult = Chain.State.CreateNewHeadCandidate(Chain, TransactionPool, Chain.State.PreviousHeaders[0], Chain.Configuration.CurrentTimestamp, Coinbase, ExtraData, MinimumGasPrice);

                // Restore our coverage state and tracing state.
                Chain.Configuration.CodeCoverage.Enabled = oldCoverageState;
                Chain.Configuration.DebugConfiguration.IsTracing = oldTracingState;

                // We mine our block and do our proof of work to reach desired nonce/mix hash which satisfies our difficulty condition.
                (byte[] Nonce, byte[] MixHash) miningResult = Chain.Configuration.IgnoreEthashVerification ? (new byte[8], new byte[32]) : MiningPoW.Mine(candidateResult.newBlock.Header, 0, ulong.MaxValue);

                // Set our nonce and mix hash for this block
                candidateResult.newBlock.Header.MixHash = miningResult.MixHash;
                candidateResult.newBlock.Header.Nonce = miningResult.Nonce;

                // Add it to our chain
                Chain.AddBlock(candidateResult.newBlock, TrustMinerStates ? candidateResult.newState : null);
            }

            // Exit critical section
            _chainUpdateLock.Release();
        }

        public void QueueTransaction(Transaction transaction)
        {
            // Push our transaction into our pool.
            TransactionPool.Push(transaction);
            MiningUpdate();
        }
        #endregion
    }
}
