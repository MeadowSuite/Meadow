using Meadow.Core.AbiEncoding;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Meadow.Contract
{
    public class EventLog
    {
        public virtual string EventName { get; }

        public virtual string EventSignature { get; }

        /// <summary>
        /// address from which this log originated
        /// </summary>
        public Address Address { get; protected set; }

        /// <summary>
        /// hash of the block where this log was in. null when its pending
        /// </summary>
        public Hash? BlockHash { get; protected set; }

        /// <summary>
        /// the block number where this log was in. null when its pending
        /// </summary>
        public ulong? BlockNumber { get; protected set; }

        /// <summary>
        /// integer of the log index position in the block
        /// </summary>
        public ulong? LogIndex { get; protected set; }

        /// <summary>
        /// contains one or more 32 Bytes non-indexed arguments of the log (ABI encoded)
        /// </summary>
        public byte[] Data { get; protected set; }

        /// <summary>
        /// Array of 0 to 4 32 Bytes DATA of indexed log arguments. 
        /// (In solidity: The first topic is the hash of the signature of the event 
        /// </summary>
        public Data[] Topics { get; protected set; }

        /// <summary>
        /// hash of the transactions this log was created from. null when its pending log
        /// </summary>
        public Hash? TransactionHash { get; protected set; }

        /// <summary>
        /// The arguments coming from the event
        /// </summary>
        public (string Name, string Type, bool Indexed, object Value)[] LogArgs { get; protected set; }

        public EventLog(FilterLogObject log)
        {
            if (log.Topics[0].GetHexString(hexPrefix: false) != EventSignature)
            {
                throw new Exception("Event signature hash does not match");
            }
            
            Address = log.Address;
            BlockHash = log.BlockHash;
            BlockNumber = log.BlockNumber;
            LogIndex = log.LogIndex;
            Data = log.Data;
            Topics = log.Topics;
            TransactionHash = log.TransactionHash;
        }

    }
    
}

