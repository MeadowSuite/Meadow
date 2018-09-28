using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using System;

namespace Meadow.Core.AbiEncoding
{

    public interface IAbiTypeEncoder
    {
        AbiTypeInfo TypeInfo { get; }

        void SetTypeInfo(AbiTypeInfo info);

        int GetEncodedSize();

        /// <summary>
        /// Encodes and writes the value to the buffer, then returns the buffer 
        /// with this position/cursor incremented to where the next writer should
        /// start at.
        /// </summary>
        void Encode(ref AbiEncodeBuffer buffer);

        void DecodeObject(ref AbiDecodeBuffer buffer, out object val);
        void SetValue(object val);

        int GetPackedEncodedSize();
        void EncodePacked(ref Span<byte> buffer);
    }

    public interface IAbiTypeEncoder<TVal> : IAbiTypeEncoder
    {
        void SetValue(in TVal val);
        void Decode(ref AbiDecodeBuffer buff, out TVal val);
    }

    public abstract class AbiTypeEncoder<TVal> : IAbiTypeEncoder<TVal>
    {
        public AbiTypeInfo TypeInfo => _info;

        protected AbiTypeInfo _info;
        protected TVal _val;

        public virtual void SetTypeInfo(AbiTypeInfo info)
        {
            _info = info;
        }

        public virtual void SetValue(in TVal val)
        {
            _val = val;
        }

        public abstract void EncodePacked(ref Span<byte> buffer);

        public abstract void Decode(ref AbiDecodeBuffer buffer, out TVal val);
        public abstract void Encode(ref AbiEncodeBuffer buffer);

        public abstract int GetEncodedSize();
        public abstract int GetPackedEncodedSize();

        protected Exception UnsupportedTypeException()
        {
            return EncoderUtil.CreateUnsupportedTypeEncodingException(_info);
        }

        protected int PadLength(int len, int multiple)
        {
            // Determine how much is divisible as a double.
            double quotient = (double)len / multiple;

            // Apply a ceiling operation to it, rounding up if it is not perfectly divisible, and scale it to our multiple.
            return (int)Math.Ceiling(quotient) * multiple;
        }

        public void DecodeObject(ref AbiDecodeBuffer buff, out object val)
        {
            TVal result;
            Decode(ref buff, out result);
            val = result;
        }

        public virtual void SetValue(object val)
        {
            if (!typeof(TVal).IsAssignableFrom(val.GetType()))
            {
                throw new Exception($"The provided type {val.GetType()} must be assignable to type {typeof(TVal)}");
            }

            TVal item = TypeConversion.ConvertValue<TVal>(val);
            SetValue(item);
        }


        protected void ThrowInvalidTypeException(object val)
        {
            throw new ArgumentException($"Cannot encode value [{val.GetType()}] '{val}' as solidity type '{TypeInfo.SolidityName}'");
        }
    }

}
