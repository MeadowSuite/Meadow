using Meadow.Core.RlpEncoding;
using Meadow.EVM.EVM.Definitions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Data_Types.Transactions
{
    /// <summary>
    /// Receipts are generated in response to applying a transaction, and give the resulting state root hash, gas used during the transaction, logs for events called, and a bloom filter to quickly index logs.
    /// </summary>
    public class Receipt : IRLPSerializable
    {
        #region Properties
        /// <summary>
        /// Represents the state trie root hash after execution.
        /// </summary>
        public byte[] StateRoot { get; private set; }
        /// <summary>
        /// Represents the block gas used at the time the transaction was processed in the block.
        /// </summary>
        public BigInteger GasUsed { get; private set; }
        /// <summary>
        /// Represents the bloom filter generated for the logs for quick lookup of possible inclusiveness.
        /// </summary>
        public BigInteger Bloom { get; private set; }
        /// <summary>
        /// Represents the logs after execution/application of the transaction.
        /// </summary>
        public List<Log> Logs { get; private set; }
        #endregion

        #region Contructors
        /// <summary>
        /// Our default constructor.
        /// </summary>
        public Receipt()
        {
            Logs = new List<Log>();
        }

        /// <summary>
        /// Creates a receipt instance with the given values.
        /// </summary>
        /// <param name="stateRoot">The state root to set upon creation.</param>
        /// <param name="gasUsed">The gas used to set upon creation.</param>
        /// <param name="logs">The logs list to set upon creation.</param>
        public Receipt(byte[] stateRoot, BigInteger gasUsed, List<Log> logs)
        {
            StateRoot = stateRoot;
            GasUsed = gasUsed;
            Logs = new List<Log>(logs);
            GenerateBloomFilter();
        }

        /// <summary>
        /// Creates a receipt instance given an RLP serialized Receipt we can decode to obtain values for.
        /// </summary>
        /// <param name="rlpReceipt">The RLP serialized receipt to decode and set values from.</param>
        public Receipt(RLPItem rlpReceipt)
        {
            Deserialize(rlpReceipt);
        }
        #endregion

        #region Functions
        /// <summary>
        /// Generates an updated bloom filter and updates the receipt bloom property.
        /// </summary>
        private void GenerateBloomFilter()
        {
            // We can loop through all logs, and generate bloom filters for the address and log list, and OR them together (to combine all the bits set used for inclusiveness checks).
            BigInteger bloomFilter = 0;
            foreach (Log log in Logs)
            {
                bloomFilter |= BloomFilter.Generate(log.Address, Addressing.Address.ADDRESS_SIZE);
                bloomFilter |= BloomFilter.Generate(log.Topics, EVMDefinitions.WORD_SIZE);
            }

            // Set our result.
            Bloom = bloomFilter;
        }
        #endregion

        #region RLP Serialization
        /// <summary>
        /// Serializes the receipt into an RLP item for encoding.
        /// </summary>
        /// <returns>Returns a serialized RLP receipt.</returns>
        public RLPItem Serialize()
        {
            // We create a new RLP list that constitute this receipt.
            RLPList rlpReceipt = new RLPList();

            // Add our state root, gas used
            rlpReceipt.Items.Add(StateRoot);
            rlpReceipt.Items.Add(RLP.FromInteger(GasUsed, EVMDefinitions.WORD_SIZE, true));

            // Generate a new bloom filter, and add that.
            GenerateBloomFilter();
            rlpReceipt.Items.Add(RLP.FromInteger(Bloom, EVMDefinitions.BLOOM_FILTER_SIZE));
       
            // Add our RLP encoded logs.
            RLPList rlpLogs = new RLPList();
            foreach (Log log in Logs)
            {
                rlpLogs.Items.Add(log.Serialize());
            }

            rlpReceipt.Items.Add(rlpLogs);

            // Return our rlp receipt item.
            return rlpReceipt;
        }

        /// <summary>
        /// Deserializes the given RLP serialized receipt and sets all values accordingly.
        /// </summary>
        /// <param name="item">The RLP item to deserialize and obtain values from.</param>
        public void Deserialize(RLPItem item)
        {
            // Verify this is a list
            if (!item.IsList)
            {
                throw new ArgumentException();
            }

            // Verify it has 4 items.
            RLPList rlpReceipt = (RLPList)item;
            if (rlpReceipt.Items.Count != 4)
            {
                throw new ArgumentException();
            }

            // Verify the types of all items
            if (!rlpReceipt.Items[0].IsByteArray ||
                !rlpReceipt.Items[1].IsByteArray ||
                !rlpReceipt.Items[2].IsByteArray ||
                !rlpReceipt.Items[3].IsList)
            {
                throw new ArgumentException();
            }

            // Set our state root
            RLPByteArray rlpStateRoot = (RLPByteArray)rlpReceipt.Items[0];
            StateRoot = rlpStateRoot.Data.ToArray();

            // Set our gas used
            RLPByteArray rlpGasUsed = (RLPByteArray)rlpReceipt.Items[1];
            GasUsed = RLP.ToInteger(rlpGasUsed, EVMDefinitions.WORD_SIZE);

            // Set our bloom
            RLPByteArray rlpBloom = (RLPByteArray)rlpReceipt.Items[2];
            Bloom = RLP.ToInteger(rlpBloom, EVMDefinitions.BLOOM_FILTER_SIZE);

            // Obtain our logs
            RLPList rlpLogs = (RLPList)rlpReceipt.Items[3];
            Logs = new List<Log>();
            foreach (RLPItem rlpLog in rlpLogs.Items)
            {
                // Add our log
                Logs.Add(new Log(rlpLog));
            }
        }
        #endregion
    }
}
