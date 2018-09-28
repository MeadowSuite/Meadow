using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.Storage
{
    /// <summary>
    /// Represents a location/pointer in storage to a variable's data.
    /// </summary>
    public class StorageLocation
    {
        #region Fields
        private BigInteger _slotKeyInteger;
        private Memory<byte> _slotKey;
        #endregion

        #region Properties
        /// <summary>
        /// Represents a storage key used to obtain a storage value (as an integer).
        /// </summary>
        public BigInteger SlotKeyInteger
        {
            get
            {
                return _slotKeyInteger;
            }
            set
            {
                // Set our values.
                _slotKeyInteger = value.CapOverflow(UInt256.SIZE, false);
                _slotKey = BigIntegerConverter.GetBytes(_slotKeyInteger, UInt256.SIZE);
            }
        }

        /// <summary>
        /// Represents a storage key used to obtain a storage value (as bytes).
        /// </summary>
        public Memory<byte> SlotKey
        {
            get
            {
                return _slotKey;
            }
            set
            {
                // Set our values.
                _slotKey = value;
                _slotKeyInteger = BigIntegerConverter.GetBigInteger(value.Span, false, UInt256.SIZE);
            }
        }

        /// <summary>
        /// Represents the offset in the obtained storage value where we wish to point to.
        /// </summary>
        public int DataOffset { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the storage location pointer using the given slot key and data offset.
        /// </summary>
        /// <param name="slotKeyInteger">The storage key used to obtain a storage value.</param>
        /// <param name="dataOffset">The offset in the obtained storage value where we wish to point to.</param>
        public StorageLocation(BigInteger slotKeyInteger, int dataOffset)
        {
            // Set our properties
            SlotKeyInteger = slotKeyInteger;
            DataOffset = dataOffset;
        }

        /// <summary>
        /// Initializes the storage location pointer using the given slot key and data offset.
        /// </summary>
        /// <param name="slotKey">The storage key used to obtain a storage value.</param>
        /// <param name="dataOffset">The offset in the obtained storage value where we wish to point to.</param>
        public StorageLocation(Memory<byte> slotKey, int dataOffset)
        {
            // Set our properties
            SlotKey = slotKey;
            DataOffset = dataOffset;
        }
        #endregion
    }
}
