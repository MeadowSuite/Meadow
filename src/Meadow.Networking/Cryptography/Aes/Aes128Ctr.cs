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
    public class Aes128Ctr : ICryptoTransform
    {
        /*
         * AES-128-CTR encrypts data by xoring queued bytes from a big endian integer byte buffer 
         * which is incremented (counter) as more bytes are needed for xoring/crypt operations. 
         */
        #region Constants
        public const int BLOCK_SIZE = 16; // in bytes
        public const int KEY_SIZE = 16; // in bytes
        public const int IV_SIZE = 16; // in bytes
        #endregion

        #region Fields
        private ICryptoTransform _aesProvider;
        private SymmetricAlgorithm _algorithm;
        private byte[] _counter;
        private Queue<byte> _counterHashBytesQueue;
        #endregion

        #region Properties
        public bool CanReuseTransform => false;

        public bool CanTransformMultipleBlocks => true;

        public int InputBlockSize => throw new NotImplementedException();

        public int OutputBlockSize => throw new NotImplementedException();
        public IReadOnlyCollection<byte> Counter => _counter;
        #endregion

        #region Constructor
        public Aes128Ctr(byte[] key, byte[] counter = null)
        {
            // Set the algorithm used
            _algorithm = new AesManaged()
            {
                KeySize = KEY_SIZE * 8,
                BlockSize = BLOCK_SIZE * 8,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };

            // Set the counter
            _counter = counter != null ? (byte[])counter.Clone() : new byte[BLOCK_SIZE];

            // Verify the counter size
            if (_counter.Length != _algorithm.BlockSize / 8)
            {
                throw new ArgumentException($"Counter provided to AES-128-CTR must be of equal to the block size of {BLOCK_SIZE}");
            }

            // Initialize the queue for hash bytes to cipher with
            _counterHashBytesQueue = new Queue<byte>();

            // Create our internal encryption provider.
            _aesProvider = _algorithm.CreateEncryptor(key, new byte[_algorithm.KeySize / 8]);
        }
        #endregion

        #region Functions
        public static byte[] Encrypt(byte[] key, byte[] data, byte[] counter = null)
        {
            // Create a new aes-128-ctr provider.
            Aes128Ctr aes = new Aes128Ctr(key, counter);
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
                if (_counterHashBytesQueue.Count == 0)
                {
                    // Create a buffer to output the transformed block.
                    byte[] transformedBlock = new byte[_algorithm.BlockSize / 8];

                    // Transform the counter block.
                    _aesProvider.TransformBlock(_counter, 0, _counter.Length, transformedBlock, 0);

                    // Queue all bytes from the transformed block
                    for (int x = 0; x < transformedBlock.Length; x++)
                    {
                        _counterHashBytesQueue.Enqueue(transformedBlock[x]);
                    }

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
                outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ _counterHashBytesQueue.Dequeue());
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
