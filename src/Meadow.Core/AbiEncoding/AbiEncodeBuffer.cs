using Meadow.Core.Utils;
using System;
using Meadow.Core.EthTypes;

namespace Meadow.Core.AbiEncoding
{
    public ref struct AbiEncodeBuffer
    {
        /// <summary>
        /// Length of the header section of the buffer
        /// </summary>
        public int HeadLength;
        public Span<byte> HeadCursor;

        public int DataAreaCursorPosition;
        public Span<byte> DataAreaCursor;

        public AbiEncodeBuffer(string hexString, params AbiTypeInfo[] typeInfo)
            : this(hexString.HexToSpan(), typeInfo) { }

        public AbiEncodeBuffer(Memory<byte> mem, params AbiTypeInfo[] typeInfo)
            : this(mem.Span, typeInfo) { }

        public AbiEncodeBuffer(Span<byte> span, params AbiTypeInfo[] typeInfo)
        {
            HeadLength = 0;

            foreach (var t in typeInfo)
            {
                switch (t.Category)
                {
                    case SolidityTypeCategory.FixedArray:
                        int totalArraySize = 1;
                        foreach (var dim in t.ArrayDimensionSizes)
                        {
                            totalArraySize *= dim;
                        }

                        HeadLength += UInt256.SIZE * totalArraySize;
                        break;
                    default:
                        HeadLength += UInt256.SIZE;
                        break;
                }
            }

            HeadCursor = span.Slice(0, HeadLength);

            DataAreaCursorPosition = 0;
            DataAreaCursor = span.Slice(HeadLength);
        }

        public void IncrementHeadCursor(int len)
        {
            HeadCursor = HeadCursor.Slice(len);
        }

        public void IncrementDataCursor(int len)
        {
            DataAreaCursorPosition += len;
            DataAreaCursor = DataAreaCursor.Slice(len);
        }
    }

}
