using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Definitions
{
    /// <summary>
    /// Represents generic EVM definitions that are desirable to access globally.
    /// </summary>
    public abstract class EVMDefinitions
    {
        #region Constants
        /// <summary>
        /// Represents the size (in bytes) of a WORD in the Ethereum Virtual Machine.
        /// </summary>
        public const int WORD_SIZE = 0x20;
        /// <summary>
        /// Represents the size (in bits) of a WORD in the Ethereum Virtual Machine.
        /// </summary>
        public const int WORD_SIZE_BITS = WORD_SIZE * 8;
        /// <summary>
        /// Represents the maximum depth allowed when calling functions in the Ethereum Virtual Machine.
        /// </summary>
        public const int MAX_CALL_DEPTH = 1024;
        /// <summary>
        /// Represents the maximum byte size of a contract.
        /// </summary>
        public const int MAX_CONTRACT_SIZE = 0x6000;
        /// <summary>
        /// Represents the size of a bloom filter in bytes.
        /// </summary>
        public const int BLOOM_FILTER_SIZE = 0x100;
        #endregion

        #region Fields
        private static BigInteger? _uint256_max_value;
        private static BigInteger? _int256_max_value;
        private static BigInteger? _int256_min_value;
        #endregion

        #region Properties
        /// <summary>
        /// Represents the minimum value a 256-bit unsigned integer could have.
        /// </summary>
        public static BigInteger UINT256_MIN_VALUE
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Represents the maximum value a 256-bit unsigned integer could have.
        /// </summary>
        public static BigInteger UINT256_MAX_VALUE
        {
            get
            {
                return _uint256_max_value.Value;
            }
        }

        /// <summary>
        /// Represents the minimum value a 256-bit signed integer could have.
        /// </summary>
        public static BigInteger INT256_MAX_VALUE
        {
            get
            {
                return _int256_max_value.Value;
            }
        }

        /// <summary>
        /// Represents the maximum value a 256-bit signed integer could have.
        /// </summary>
        public static BigInteger INT256_MIN_VALUE
        {
            get
            {
                return _int256_min_value.Value;
            }
        }
        #endregion

        #region Constructors
        static EVMDefinitions()
        {
            _uint256_max_value = 1;
            _uint256_max_value <<= 256;
            _uint256_max_value -= 1;

            _int256_max_value = 1;
            _int256_max_value <<= 255;
            _int256_max_value -= 1;

            _int256_min_value = -1 - INT256_MAX_VALUE;
        }
        #endregion

        #region Functions
        public static BigInteger GetWordCount(BigInteger size)
        {
            BigInteger targetWordCount = (size + WORD_SIZE - 1) / WORD_SIZE;
            return targetWordCount;
        }
        #endregion
    }
}
