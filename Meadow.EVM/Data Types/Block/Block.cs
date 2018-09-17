using Meadow.Core.Cryptography;
using Meadow.Core.RlpEncoding;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Transactions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Data_Types.Block
{
    public class Block : IRLPSerializable
    {
        #region Properties
        public BlockHeader Header { get; set; }
        public Transaction[] Transactions { get; set; }
        public BlockHeader[] Uncles { get; set; }

        public static byte[] BLANK_UNCLES_HASH
        {
            get
            {
                // RLP encode our a blank list and hash it.
                return KeccakHash.ComputeHashBytes(RLP.Encode(new RLPList()));
            }
        }
        #endregion

        #region Constructor
        public Block() { }
        public Block(BlockHeader header, Transaction[] transactions, BlockHeader[] uncles)
        {
            // Set all of our properties
            Header = header;
            Transactions = transactions;
            Uncles = uncles;
        }

        public Block(RLPItem rlpBlockHeader)
        {
            Deserialize(rlpBlockHeader);
        }
        #endregion

        #region Functions
        /// <summary>
        /// Calculates the difficulty for the next block given a previous blocks header, the timestamp of the new block, and the configuration.
        /// </summary>
        /// <param name="parentBlockHeader">The previous block header from which we derive difficulty.</param>
        /// <param name="timestamp">The timestamp of the new block.</param>
        /// <param name="configuration">The configuration under which we calculate difficulty.</param>
        /// <returns>Returns the difficulty for the supposed new block.</returns>
        public static BigInteger CalculateDifficulty(BlockHeader parentBlockHeader, BigInteger timestamp, Configuration.Configuration configuration)
        {
            // Handle the special case of difficulty for test chains (difficulty 1 keeps difficulty at 1).
            if (parentBlockHeader.Difficulty == 1)
            {
                return 1;
            }

            // Check our versioning
            BigInteger newBlockNumber = parentBlockHeader.BlockNumber + 1;
            bool isByzantium = newBlockNumber >= configuration.GetReleaseStartBlockNumber(Configuration.EthereumRelease.Byzantium);
            bool isHomestead = newBlockNumber >= configuration.GetReleaseStartBlockNumber(Configuration.EthereumRelease.Homestead);

            // TODO: Revisit the rest of this. Currently a poor implementation. Use following as source: https://dltlabs.com/how-difficulty-adjustment-algorithm-works-in-ethereum/
            BigInteger offset = parentBlockHeader.Difficulty / configuration.DifficultyFactor;
            BigInteger sign = 0;
            BigInteger deltaTime = timestamp - parentBlockHeader.Timestamp;
            if (isByzantium)
            {
                BigInteger x = 2;
                if (parentBlockHeader.UnclesHash.ValuesEqual(BLANK_UNCLES_HASH))
                {
                    x = 1;
                }

                sign = BigInteger.Max(-99, x - (deltaTime / configuration.DifficultyAdjustmentCutOffByzantium));
            }
            else if (isHomestead)
            {
                sign = BigInteger.Max(-99, 1 - (deltaTime / configuration.DifficultyAdjustmentCutOffHomestead));
            }
            else
            {
                if (deltaTime < configuration.DifficultyAdjustmentCutOff)
                {
                    sign = 1;
                }
                else
                {
                    sign = -1;
                }
            }

            BigInteger minDifficulty = BigInteger.Min(configuration.MinDifficulty, parentBlockHeader.Difficulty);
            BigInteger difficulty = BigInteger.Max(minDifficulty, parentBlockHeader.Difficulty + (offset * sign));
            BigInteger periods = newBlockNumber / configuration.DifficultyExponentialPeriod;
            if (isByzantium)
            {
                periods -= configuration.DifficultyExponentialFreePeriodsByzantium;
            }

            if (periods >= configuration.DifficultyExponentialFreePeriods)
            {
                BigInteger exponent = periods - configuration.DifficultyExponentialFreePeriods;
                return BigInteger.Max(difficulty + BigInteger.Pow(2, (int)exponent), configuration.MinDifficulty);
            }

            return difficulty;
        }

        /// <summary>
        /// Calculates the hash for the RLP encoded uncle array.
        /// </summary>
        /// <returns>Returns the hash of the RLP encoded uncle array.</returns>
        public byte[] CalculateUnclesHash()
        {
            // We create an RLP list with all of our uncle block headers
            RLPList rlpUncles = new RLPList();
            foreach (BlockHeader uncleHeader in Uncles)
            {
                rlpUncles.Items.Add(uncleHeader.Serialize());
            }

            // RLP encode our uncles and hash them
            return KeccakHash.ComputeHashBytes(RLP.Encode(rlpUncles));
        }
        #endregion

        #region RLP Serialization
        /// <summary>
        /// Serializes the block into an RLP item for encoding.
        /// </summary>
        /// <returns>Returns a serialized RLP block.</returns>
        public RLPItem Serialize()
        {
            // We create a new RLP list that constitute this header.
            RLPList rlpBlock = new RLPList();

            // Add our header
            rlpBlock.Items.Add(Header.Serialize());

            // Add all of our transactions
            RLPList rlpTransactions = new RLPList();
            foreach (Transaction transaction in Transactions)
            {
                rlpTransactions.Items.Add(transaction.Serialize());
            }

            rlpBlock.Items.Add(rlpTransactions);

            // Add all of our uncle block headers
            RLPList rlpUncles = new RLPList();
            foreach (BlockHeader uncleHeader in Uncles)
            {
                rlpUncles.Items.Add(uncleHeader.Serialize());
            }

            rlpBlock.Items.Add(rlpUncles);

            // Return our rlp header item.
            return rlpBlock;
        }

        /// <summary>
        /// Deserializes the given RLP serialized block and sets all values accordingly.
        /// </summary>
        /// <param name="item">The RLP item to deserialize and obtain values from.</param>
        public void Deserialize(RLPItem item)
        {
            // Verify this is a list
            if (!item.IsList)
            {
                throw new ArgumentException();
            }

            // Verify it has 3 items.
            RLPList rlpBlock = (RLPList)item;
            if (rlpBlock.Items.Count != 3)
            {
                throw new ArgumentException();
            }

            // Verify the types of all items (should all be lists)
            for (int i = 0; i < rlpBlock.Items.Count; i++)
            {
                if (!rlpBlock.Items[i].IsList)
                {
                    throw new ArgumentException();
                }
            }

            // Obtain the block header
            Header = new BlockHeader(rlpBlock.Items[0]);

            // Obtain the list of transactions
            RLPList rlpTransactions = (RLPList)rlpBlock.Items[1];
            Transactions = new Transaction[rlpTransactions.Items.Count];
            for (int i = 0; i < rlpTransactions.Items.Count; i++)
            {
                Transactions[i] = new Transaction(rlpTransactions.Items[i]);
            }

            // Obtain the list of uncles.
            RLPList rlpUncles = (RLPList)rlpBlock.Items[2];
            Uncles = new BlockHeader[rlpUncles.Items.Count];
            for (int i = 0; i < rlpUncles.Items.Count; i++)
            {
                Uncles[i] = new BlockHeader(rlpUncles.Items[i]);
            }
        }
        #endregion
    }
}
