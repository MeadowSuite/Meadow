using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Meadow.Core.AbiEncoding
{
    public static class EncoderUtil
    {
        public static Exception CreateUnsupportedTypeEncodingException(AbiTypeInfo typeInfo)
        {
            return new ArgumentException($"Encoder does not support solidity type category '{typeInfo.Category}', type name: {typeInfo.SolidityName}");
        }

        public static byte[] GetFunctionCallBytes(string funcSignature, params IAbiTypeEncoder[] encoders)
        {
            // get length of all encoded params
            int totalLen = GetEncodedLength(encoders);

            // add 4 bytes for function signature
            totalLen += 4;

            // create buffer to write encoded params into
            byte[] bytes = new byte[totalLen];
            Memory<byte> data = bytes;

            AbiSignature.GetMethodID(data.Span, funcSignature);

            AbiEncodeBuffer buffer = new AbiEncodeBuffer(data.Slice(4), encoders.GetTypeInfo());

            // encode transaction arguments
            WriteParams(encoders, ref buffer);

            return bytes;
        }

        public static byte[] EncodePacked(params IAbiTypeEncoder[] encoders)
        {
            int totalLen = GetPackedEncodedLength(encoders);
            var bytes = new byte[totalLen];
            Span<byte> cursor = bytes;

            foreach (var encoder in encoders)
            {
                encoder.EncodePacked(ref cursor);
            }

            return bytes;
        }

        public static byte[] Encode(params IAbiTypeEncoder[] encoders)
        {
            // get length of all encoded params
            int totalLen = GetEncodedLength(encoders);

            // create buffer to write encoded params into
            var bytes = new byte[totalLen];
            Memory<byte> data = new Memory<byte>(bytes);

            AbiEncodeBuffer buffer = new AbiEncodeBuffer(data, encoders.GetTypeInfo());

            // encode transaction arguments
            WriteParams(encoders, ref buffer);

            return bytes;
        }

        public static AbiTypeInfo[] GetTypeInfo(this IAbiTypeEncoder[] encoders)
        {
            AbiTypeInfo[] info = new AbiTypeInfo[encoders.Length];
            for (var i = 0; i < encoders.Length; i++)
            {
                info[i] = encoders[i].TypeInfo;
            }

            return info;
        }

        public static string GetHex(params IAbiTypeEncoder[] encoders)
        {
            // get length of all encoded params
            int totalLen = GetEncodedLength(encoders);

            // create buffer to write encoded params into
            Span<byte> data = stackalloc byte[totalLen];
            AbiEncodeBuffer buff = new AbiEncodeBuffer(data, encoders.GetTypeInfo());

            // encode transaction arguments
            WriteParams(encoders, ref buff);

            // hex encode
            return HexUtil.GetHexFromBytes(data, hexPrefix: true);
        }

        public static string ToEncodedHex(this IAbiTypeEncoder encoder)
        {
            return GetHex(encoder);
        }

        public static int GetEncodedLength(params IAbiTypeEncoder[] encoders)
        {
            // get length of all encoded params
            int totalLen = 0;
            foreach (var encoder in encoders)
            {
                var len = encoder.GetEncodedSize();
                Debug.Assert(len % 32 == 0);
                totalLen += len;
            }

            return totalLen;
        }

        public static int GetPackedEncodedLength(params IAbiTypeEncoder[] encoders)
        {
            int totalLen = 0;
            foreach (var encoder in encoders)
            {
                var len = encoder.GetPackedEncodedSize();
                totalLen += len;
            }

            return totalLen;
        }

        static void WriteParams(IAbiTypeEncoder[] encoders, ref AbiEncodeBuffer buff)
        {
            // encode transaction arguments
            foreach (var encoder in encoders)
            {
                encoder.Encode(ref buff);
            }
        }
    }
}
