using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Meadow.Networking.Cryptography.Aes
{
    /// <summary>
    /// Provides AES-128-CTR cryptographic implementation.
    /// </summary>
    public class AesCtr : ICryptoTransform
    {
        /*
         * AES-CTR encrypts data by xoring queued bytes from a big endian integer byte buffer 
         * which is incremented (counter) as more bytes are needed for xoring/crypt operations. 
         */
        #region Constants
        public const int BLOCK_SIZE = 16; // in bytes (128-bit)
        #endregion

        #region Fields
        private ICryptoTransform _aesProvider;
        private SymmetricAlgorithm _algorithm;
        private byte[] _counter;
        private byte[] _counterHashBuffer;
        private int _counterHashIndex;
        #endregion

        #region Properties
        public bool CanReuseTransform => false;

        public bool CanTransformMultipleBlocks => true;

        public int InputBlockSize => BLOCK_SIZE;

        public int OutputBlockSize => BLOCK_SIZE;
        public IReadOnlyCollection<byte> Counter => _counter;
        public int KeySize
        {
            get
            {
                return _algorithm.KeySize;
            }
        }
        #endregion

        #region Constructor
        public AesCtr(byte[] key, byte[] counter = null)
        {
            // Verify the size of the data
            int keyBitSize = key.Length * 8;
            if (keyBitSize != 128 && keyBitSize != 192 && keyBitSize != 256)
            {
                throw new ArgumentException($"Key provided to AES-CTR must be of size 128, 192 or 256. Provided size: {keyBitSize}.");
            }

            // Set the algorithm used
            _algorithm = new AesManaged()
            {
                KeySize = keyBitSize,
                BlockSize = BLOCK_SIZE * 8,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };

            // Set the counter
            _counter = counter != null ? (byte[])counter.Clone() : new byte[BLOCK_SIZE];

            // Verify the counter size
            if (_counter.Length != _algorithm.BlockSize / 8)
            {
                throw new ArgumentException($"Counter provided to AES-{keyBitSize}-CTR must be of equal to the block size of {BLOCK_SIZE}");
            }

            // Initialize the buffer for our counter hash which we xor with input data to encrypted/decrypt.
            _counterHashBuffer = new byte[BLOCK_SIZE];

            // Set our counter hash index to the end of the buffer so it is re-evaluated immediately
            _counterHashIndex = _counterHashBuffer.Length;


            // Create our internal encryption provider.
            _aesProvider = _algorithm.CreateEncryptor(key, new byte[BLOCK_SIZE]);
        }
        #endregion

        #region Functions
        public static byte[] Encrypt(byte[] key, byte[] data, byte[] counter = null)
        {
            // Create a new aes-128-ctr provider.
            AesCtr aes = new AesCtr(key, counter);
            return aes.TransformFinalBlock(data, 0, data.Length);
        }

        public static byte[] Decrypt(byte[] key, byte[] data,  byte[] counter = null)
        {
            return Encrypt(key, data, counter);
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            // Cap the input count
            inputCount = Math.Min(inputCount, inputBuffer.Length - inputOffset);
            inputCount = Math.Min(inputCount, outputBuffer.Length - outputOffset);
            inputCount = Math.Max(0, inputCount);

            // Loop for each byte to xor.
            int endOffset = outputOffset + inputCount;
            for (int i = 0; i < inputCount; i++)
            {
                // If we have no more queued bytes, we increment our counter and hash it.
                if (_counterHashIndex == _counterHashBuffer.Length)
                {
                    // Reset our counter hash index to the start of the buffer.
                    _counterHashIndex = 0;

                    // Encrypt the counter and put the output in our counter hash buffer.
                    _aesProvider.TransformBlock(_counter, 0, _counter.Length, _counterHashBuffer, 0);

                    // The counter must be incremented. It represents an integer in big-endian order.
                    for (int x = _counter.Length - 1; x >= 0; x--)
                    {
                        // Increment the indexed byte, determine if we should carry to the next byte.
                        bool carry = ++_counter[x] == 0;

                        // If we don't need to carry, we don't increment any more bytes.
                        if (!carry)
                        {
                            break;
                        }
                    }
                }

                // Perform a xor operation on our indexed byte 
                outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ _counterHashBuffer[_counterHashIndex++]);
            }

            // Return the amount of bytes read.
            return inputCount;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            // Create a buffer for the output buffer
            byte[] outputBuffer = new byte[inputCount];

            // Transform the data
            TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, 0);

            // Return the output data.
            return outputBuffer;
        }

        public void Dispose()
        {
            // Dispose the underlying transform provider.
            _aesProvider.Dispose();
        }
        #endregion
    }
}
