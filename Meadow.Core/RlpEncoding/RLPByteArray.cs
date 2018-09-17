using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Core.RlpEncoding
{
    /// <summary>
    /// Represents a byte array which can be RLP serialized.
    /// </summary>
    public class RLPByteArray : RLPItem
    {
        #region Properties
        /// <summary>
        /// The embedded raw data inside of this RLP item.
        /// </summary>
        public Memory<byte> Data { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor, initializes with a null data array.
        /// </summary>
        public RLPByteArray() { }
        /// <summary>
        /// Initializes an RLP byte array with the given data.
        /// </summary>
        /// <param name="data">The data to set for our RLP item.</param>
        public RLPByteArray(Memory<byte> data)
        {
            Data = data;
        }
        #endregion
    }
}
