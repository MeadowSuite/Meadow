using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Core.Utils
{
    public class BitStream : Stream
    {
        #region Constants
        private const int BITS_PER_BYTE = 8;
        private const int BITS_PER_USHORT = BITS_PER_BYTE * 2;
        private const int BITS_PER_UINT = BITS_PER_BYTE * 4;
        private const int BITS_PER_ULONG = BITS_PER_BYTE * 8;
        #endregion

        #region Fields
        private Stream _stream;
        private int _bitPosition;
        private static readonly byte[] BitCountMasks =
        {
            0x00, 0x01, 0x03, 0x07, 0x0F, 0x1F, 0x3F, 0x7F, 0xFF
        };
        #endregion

        #region Properties
        /// <summary>
        /// Indicates if the stream is opened, or if it has been closed.
        /// </summary>
        public bool Opened { get; private set; }
        
        /// <summary>
        /// Indicates the bit position in the current byte we are writing to, counting bit 0 as the most significant bit (left most bit).
        /// Setting this value greater than the size of a byte will advance byte position and correct the bit position relative to the correct byte.
        /// Setting this value less than zero will roll back the position and correct the bit position relative to the correct byte.
        /// </summary>
        public int BitPosition
        {
            get
            {
                // Return our positions.
                return _bitPosition;
            }
            set
            {
                // Set our stream position and bit position
                if (value >= 0)
                {
                    Position += (value / 8);
                    _bitPosition = (value % 8);
                }
                else
                {
                    int val = Math.Abs(value);
                    Position -= (int)Math.Ceiling((double)val / 8);
                    _bitPosition = BITS_PER_BYTE - (val % 8);
                }
            }
        }

        public override bool CanRead
        {
            get
            {
                return _stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _stream.CanWrite;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return _stream.CanTimeout;
            }
        }

        public override long Position
        {
            get
            {
                return _stream.Position;
            }
            set
            {
                _stream.Position = value;
            }
        }

        public override long Length
        {
            get
            {
                return _stream.Length;
            }
        }

        public decimal BitLength
        {
            get
            {
                return Length * BITS_PER_BYTE;
            }
        }

        public override int ReadTimeout
        {
            get => _stream.ReadTimeout;
            set => _stream.ReadTimeout = value;
        }

        public override int WriteTimeout
        {
            get => _stream.WriteTimeout;
            set => _stream.WriteTimeout = value;
        }
        #endregion

        #region Constructor
        public BitStream() : this(new MemoryStream()) { }
        public BitStream(Stream stream)
        {
            // Set our properties.
            _stream = stream;
            Opened = true;
        }

        ~BitStream()
        {
            // On deconstruction, close our IO.
            Close();
        }
        #endregion

        #region Functions
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Loop for each byte to read.
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                // Read the indexed byte.
                int result = ReadByte();
                if (result == -1)
                {
                    return i - offset;
                }

                // Set the read byte
                buffer[i] = (byte)result;
            }

            // Return the count since we read all bytes
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Loop for each byte to write.
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                // Write the indexed byte.
                WriteByte(buffer[i]);
            }
        }

        public override int ReadByte()
        {
            // Read all the bits from our byte.
            return ReadByte(BITS_PER_BYTE);
        }

        public bool ReadBit()
        {
            return ReadByte(1) == 1;
        }

        public byte ReadByte(int bitCount, bool startSettingFromMSB = false)
        {
            // Verify our bit count
            if (bitCount < 0 || bitCount > BITS_PER_BYTE)
            {
                throw new ArgumentException($"Invalid number of bits to read supplied ({bitCount}). Must be between 0-8.");
            }
            else if (bitCount == 0)
            {
                // If we have no bits to write, we can proceed.
                return 0;
            }

            // If bit position is 0, we read the whole byte
            int result = 0;
            if (_bitPosition == 0 && bitCount == BITS_PER_BYTE)
            {
                // Read the whole byte and return it.
                result = _stream.ReadByte();
                if (result == -1)
                {
                    throw new EndOfStreamException();
                }

                return (byte)result;
            }

            // Otherwise we obtain the byte at this position to extract our bits from.
            result = _stream.ReadByte();

            // If we reached the end of the stream, return that result.
            if (result == -1)
            {
                throw new EndOfStreamException();
            }

            // Cast our read byte.
            byte readByte = (byte)result;

            // Mask out the bits prior to our bit position.
            readByte = (byte)(readByte & BitCountMasks[BITS_PER_BYTE - _bitPosition]);

            // Determine how many of our bits will make it to the current position byte.
            int remainingBitsInCurrentByte = BITS_PER_BYTE - _bitPosition;
            int firstByteBitCount = Math.Min(remainingBitsInCurrentByte, bitCount);
            int secondByteBitCount = bitCount - firstByteBitCount;

            // Shift our data all the way to the right (to zero out trailing bits), then shift it back to the correct position.
            readByte >>= (remainingBitsInCurrentByte - firstByteBitCount);
            readByte <<= secondByteBitCount;

            // If the data spans across two bytes
            if (secondByteBitCount > 0)
            {
                // We read the second byte
                result = _stream.ReadByte();

                // If we reached the end of the stream, return that result.
                if (result == -1)
                {
                    throw new EndOfStreamException();
                }

                // Cast our read byte.
                byte secondReadByte = (byte)result;

                // Get only the bits we are interested in
                secondReadByte >>= (BITS_PER_BYTE - secondByteBitCount);
            
                // Add them to our result byte.
                readByte |= secondReadByte;

                // Set our remaining bitcount as the second byte bitcount 
                bitCount = secondByteBitCount;
                _bitPosition = 0;
            }

            // If we didn't exceed the byte position..
            if (_bitPosition + bitCount < BITS_PER_BYTE)
            {
                // Go back to the byte, and advance the bit position
                _stream.Position--;
            }

            // Set our bit position.
            _bitPosition = (_bitPosition + bitCount) % BITS_PER_BYTE;

            // If we are to start from the most significant byte, we'll want to shift our value
            if (startSettingFromMSB)
            {
                readByte <<= BITS_PER_BYTE - (firstByteBitCount + secondByteBitCount);
            }

            // Return our read byte
            return readByte;
        }

        public byte[] ReadBytes(int count, bool countRefersToBits = false)
        {
            // If the count doesn't refer to a bit count, we adjust it to do so.
            if (!countRefersToBits)
            {
                count *= BITS_PER_BYTE;
            }

            // Determine our byte count
            int byteCount = (int)Math.Ceiling((double)count / BITS_PER_BYTE);

            // Define our array
            byte[] data = new byte[byteCount];

            // Loop for each byte to read.
            for (int i = 0; i < data.Length && count > 0; i++)
            {
                // Determine how many bits we'll read in this byte
                int bitsInCurrent = Math.Min(count, BITS_PER_BYTE);

                // Subtract from our remaining bit count.
                count -= bitsInCurrent;

                // Write our byte
                data[i] = ReadByte(bitsInCurrent, true);
            }

            // Return our array
            return data;
        }

        public short ReadInt16(int bitCount)
        {
            // Read an unsigned int and convert it to a signed int.
            return Convert.ToInt16(ReadUInt16(bitCount));
        }

        public ushort ReadUInt16(int bitCount)
        {
            // Verify the bitcount
            if (bitCount < 0 || bitCount > BITS_PER_USHORT)
            {
                throw new ArgumentException($"Invalid number of bits to read supplied ({bitCount}). Must be between 0-{BITS_PER_USHORT}.");
            }

            // Wrap the bigger integer read function and cast to our smaller integer type.
            return (ushort)ReadUInt64(bitCount);
        }

        public int ReadInt32(int bitCount)
        {
            // Read an unsigned int and convert it to a signed int.
            return Convert.ToInt32(ReadUInt32(bitCount));
        }

        public uint ReadUInt32(int bitCount)
        {
            // Verify the bitcount
            if (bitCount < 0 || bitCount > BITS_PER_UINT)
            {
                throw new ArgumentException($"Invalid number of bits to read supplied ({bitCount}). Must be between 0-{BITS_PER_UINT}.");
            }

            // Wrap the bigger integer read function and cast to our smaller integer type.
            return (uint)ReadUInt64(bitCount);
        }

        public long ReadInt64(int bitCount)
        {
            // Read an unsigned long and convert it to a signed long.
            return Convert.ToInt64(ReadUInt64(bitCount));
        }

        public ulong ReadUInt64(int bitCount)
        {
            // Verify the bitcount
            if (bitCount < 0 || bitCount > BITS_PER_ULONG)
            {
                throw new ArgumentException($"Invalid number of bits to read supplied ({bitCount}). Must be between 0-{BITS_PER_ULONG}.");
            }

            // Declare our result.
            ulong result = 0;

            // Loop for our bitcount to write
            int remainingBits = bitCount;
            while (remainingBits > 0)
            {
                // Determine how many bits are going to be in this round.
                int bitsInRound = Math.Min(remainingBits, BITS_PER_BYTE);

                // Read the bits for this round.
                ulong currentRoundBits = ReadByte(bitsInRound);

                // Advance our current index
                remainingBits -= bitsInRound;

                // Shift our current bits to the correct location and set the bits in our result.
                result |= (currentRoundBits << remainingBits);
            }

            // Return our result
            return result;
        }

        public void Write(bool bit)
        {
            WriteByte((byte)(bit ? 1 : 0), 1);
        }

        public void Write(byte value, int bitCount)
        {
            WriteByte(value, bitCount);
        }

        public void Write(short value, int bitCount)
        {
            Write(Convert.ToUInt16(value), bitCount);
        }

        public void Write(ushort value, int bitCount)
        {
            // Verify the bitcount
            if (bitCount < 0 || bitCount > BITS_PER_USHORT)
            {
                throw new ArgumentException($"Invalid number of bits to write supplied ({bitCount}). Must be between 0-{BITS_PER_USHORT}.");
            }

            Write((ulong)value, bitCount);
        }

        public void Write(int value, int bitCount)
        {
            Write(Convert.ToUInt32(value), bitCount);
        }

        public void Write(uint value, int bitCount)
        {
            // Verify the bitcount
            if (bitCount < 0 || bitCount > BITS_PER_UINT)
            {
                throw new ArgumentException($"Invalid number of bits to write supplied ({bitCount}). Must be between 0-{BITS_PER_UINT}.");
            }

            Write((ulong)value, bitCount);
        }

        public void Write(long value, int bitCount)
        {
            Write(Convert.ToUInt64(value), bitCount);
        }

        public void Write(ulong value, int bitCount)
        {
            // Verify the bitcount
            if (bitCount < 0 || bitCount > BITS_PER_ULONG)
            {
                throw new ArgumentException($"Invalid number of bits to write supplied ({bitCount}). Must be between 0-{BITS_PER_ULONG}.");
            }

            // Loop for our bitcount to write
            int remainingBits = bitCount;
            while (remainingBits > 0)
            {
                // Determine how many bits are going to be in this round.
                int bitsInRound = Math.Min(remainingBits, BITS_PER_BYTE);

                // Subtract from our remaining bit count.
                remainingBits -= bitsInRound;

                // Shift our desired bits to the correct position
                byte currentRoundBits = (byte)((value >> remainingBits) & BitCountMasks[bitsInRound]);

                // Write the bits
                WriteByte(currentRoundBits, bitsInRound);
            }
        }

        public void WriteByte(byte value, int bitCount, bool startExtractFromMSB = false)
        {
            // Verify our bit count
            if (bitCount < 0 || bitCount > BITS_PER_BYTE)
            {
                throw new ArgumentException($"Invalid number of bits to write supplied ({bitCount}). Must be between 0-8.");
            }
            else if (bitCount == 0)
            {
                // If we have no bits to write, we can proceed.
                return;
            }

            // If bit position is 0, we write the whole byte
            if (_bitPosition == 0 && bitCount == BITS_PER_BYTE)
            {
                _stream.WriteByte(value);
            }
            else
            {
                // If we are to start from the most significant byte, we'll want to shift our value
                if (startExtractFromMSB)
                {
                    value >>= BITS_PER_BYTE - bitCount;
                }

                // Otherwise we obtain the byte at this position to add our bits to.
                byte readByte = 0;
                if (Position < Length)
                {
                    readByte = (byte)_stream.ReadByte();
                    _stream.Position--;
                }

                // Obtain the bits from our byte by masking the target bits out.
                byte bitsToWrite = (byte)(value & BitCountMasks[bitCount]);

                // Determine how many of our bits will make it to the current position byte.
                int remainingBitsInCurrentByte = BITS_PER_BYTE - _bitPosition;
                int firstByteBitCount = Math.Min(remainingBitsInCurrentByte, bitCount);
                int secondByteBitCount = bitCount - firstByteBitCount;

                // If the data spans across two bytes
                if (secondByteBitCount > 0)
                {
                    // Data is MSB aligned, but if we require a second byte for some of this values
                    // bits, then we know the first byte's bits touch the LSB boundary, so we can
                    // simply mask them out and OR them in. The remainder can be treated as if
                    // we didn't need a second byte, and we were going to align towards MSB anyway.

                    readByte = (byte)(readByte & ~(BitCountMasks[firstByteBitCount]));
                    readByte = (byte)(readByte | (bitsToWrite >> secondByteBitCount));

                    // We can write the byte back to stream now
                    _stream.WriteByte(readByte);
                    _bitPosition = 0;

                    // Now we update our read byte for the next byte
                    readByte = 0;
                    if (Position < Length)
                    {
                        readByte = (byte)_stream.ReadByte();
                        _stream.Position--;
                    }

                    // Update our bits count to the second byte bit count
                    bitCount = secondByteBitCount;

                    // Obtain the bits from our byte by masking the target bits out.
                    bitsToWrite = (byte)(value & BitCountMasks[bitCount]);
                }

                // Write our byte (first if it fit, or second if we needed two bytes, either way, we'll be aligning to MSB).
                int shiftAmount = (BITS_PER_BYTE - bitCount) - _bitPosition;

                // Update our byte with our value's bits.
                readByte = (byte)(readByte & ~(BitCountMasks[bitCount] << shiftAmount));
                readByte = (byte)(readByte | (bitsToWrite << shiftAmount));

                // We can write the byte back to stream now
                _stream.WriteByte(readByte);
                
                // If we didn't exceed the byte position..
                if (_bitPosition + bitCount < BITS_PER_BYTE)
                {
                    // Go back to the byte
                    _stream.Position--;
                }
                
                // Set our bit position.
                _bitPosition = (_bitPosition + bitCount) % BITS_PER_BYTE;
            }
        }

        public void WriteBytes(byte[] data)
        {
            // Write all the bytes
            Write(data, 0, data.Length);
        }

        public void WriteBytes(byte[] data, int bitCount)
        {
            // Loop for each byte to write.
            for (int i = 0; i < data.Length && bitCount > 0; i++)
            {
                // Determine how many bits we'll write in this byte
                int bitsInCurrent = Math.Min(bitCount, BITS_PER_BYTE);

                // Subtract from our remaining bit count.
                bitCount -= bitsInCurrent;

                // Write our byte
                WriteByte(data[i], bitsInCurrent, true);
            }
        }

        public override void WriteByte(byte value)
        {
            // Write a byte
            WriteByte(value, BITS_PER_BYTE);
        }

        public override void Flush()
        {
            // Flush our internal stream
            _stream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // Flush our internal stream
            return _stream.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // If our position is an absolute position...
            if (origin == SeekOrigin.Begin || origin == SeekOrigin.End)
            {
                // Set our bit position.
                _bitPosition = 0;
            }

            // Set our byte position.
            return _stream.Seek(offset, origin);
        }

        protected override void Dispose(bool disposing)
        {
            // Dispose our underlying stream.
            _stream.Dispose();

            // Dispose this object as normal.
            base.Dispose(disposing);
        }

        public override void SetLength(long value)
        {
            // Set our internal stream's length.
            _stream.SetLength(value);
        }

        /// <summary>
        /// Resets the contents of the underlying stream to zero bytes in size, and resets position properties.
        /// </summary>
        public void ClearContents()
        {
            // Go to the start of our stream.
            Position = 0;
            BitPosition = 0;

            // Set our stream length to 0.
            SetLength(0);
        }

        public override void Close()
        {
            // Set our closed status
            Opened = false;

            // Close our internal stream
            _stream.Close();
        }

        public byte[] ToArray()
        {
            // Backup our location
            long position = Position;
            int bitPosition = BitPosition;

            // Go to the beginning.
            Position = 0;
            BitPosition = 0;

            // Read all data
            byte[] data = ReadBytes((int)Length, false);

            // Restore our location
            Position = position;
            BitPosition = bitPosition;

            // Return the array
            return data;
        }

        #region Unimplemented
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        #endregion

        #endregion
    }
}