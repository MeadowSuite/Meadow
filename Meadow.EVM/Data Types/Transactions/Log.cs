using Meadow.Core.RlpEncoding;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Definitions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Data_Types.Transactions
{
    /// <summary>
    /// Logs are used to track events emitted from within the Ethereum Virtual Machine.
    /// </summary>
    public class Log : IRLPSerializable
    {
        #region Properties
        /// <summary>
        /// The address provided from our message for our transaction log.
        /// </summary>
        public Address Address { get; private set; }
        /// <summary>
        /// Topics describe the log and what indexes information about the log (such as which event generated a log).
        /// </summary>
        public List<BigInteger> Topics { get; private set; }
        /// <summary>
        /// The data provided for the actual event/log.
        /// </summary>
        public byte[] Data { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Our default constructor.
        /// </summary>
        public Log()
        {
            Topics = new List<BigInteger>();
        }

        /// <summary>
        /// Initializes a log with the provided arguments.
        /// </summary>
        /// <param name="address">The address to set during initialization.</param>
        /// <param name="topics">The topics to set during initialization.</param>
        /// <param name="data">The data to set during initialization.</param>
        public Log(Address address, List<BigInteger> topics, byte[] data)
        {
            Address = address;
            Topics = topics;
            Data = data;
        }

        /// <summary>
        /// Creates a log instance given an RLP serialized Log we can decode to obtain values for.
        /// </summary>
        /// <param name="rlpList">The RLP serialized log to decode and set values from.</param>
        public Log(RLPItem rlpList)
        {
            Deserialize(rlpList);
        }
        #endregion

        #region RLP Serialization
        /// <summary>
        /// Serializes the log into an RLP item for encoding.
        /// </summary>
        /// <returns>Returns a serialized RLP log.</returns>
        public RLPItem Serialize()
        {
            // We create a new RLP list that constitute this log.
            RLPList rlpLog = new RLPList();

            // Add our address
            rlpLog.Items.Add(RLP.FromInteger(Address, Address.ADDRESS_SIZE));

            // Add our topics, a list of 32-bit integers.
            RLPList rlpTopicsList = new RLPList();
            foreach (BigInteger topic in Topics)
            {
                rlpTopicsList.Items.Add(RLP.FromInteger(topic, EVMDefinitions.WORD_SIZE));
            }

            rlpLog.Items.Add(rlpTopicsList);

            // Add our data
            rlpLog.Items.Add(Data);

            // Return our rlp log item.
            return rlpLog;
        }

        /// <summary>
        /// Deserializes the given RLP serialized log and sets all values accordingly.
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
            RLPList rlpLog = (RLPList)item;
            if (rlpLog.Items.Count != 3)
            {
                throw new ArgumentException();
            }

            // Verify the types of all items
            if (!rlpLog.Items[0].IsByteArray ||
                !rlpLog.Items[1].IsList ||
                !rlpLog.Items[2].IsByteArray)
            {
                throw new ArgumentException();
            }

            // Set our address
            RLPByteArray rlpAddress = (RLPByteArray)rlpLog.Items[0];
            Address = new Address(rlpAddress.Data.Span);

            // Obtain our topics
            RLPList rlpTopicsList = (RLPList)rlpLog.Items[1];
            Topics = new List<BigInteger>();
            foreach (RLPItem rlpTopic in rlpTopicsList.Items)
            {
                // Verify all of our items are data
                if (rlpTopic.GetType() != typeof(RLPByteArray))
                {
                    throw new ArgumentException();
                }

                // Add our topic.
                Topics.Add(RLP.ToInteger((RLPByteArray)rlpTopic, EVMDefinitions.WORD_SIZE));
            }

            // Obtain our data
            RLPByteArray rlpData = (RLPByteArray)rlpLog.Items[2];
            Data = rlpData.Data.ToArray();
        }
        #endregion
    }
}
