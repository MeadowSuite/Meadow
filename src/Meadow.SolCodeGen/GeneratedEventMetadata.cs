using Meadow.Core.AbiEncoding;
using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace Meadow.SolCodeGen
{

    public class GeneratedEventMetadata
    {
        public string EventSignatureHash { get; set; }
        public string EventClrTypeName { get; set; }
        public int IndexedArgsCounts { get; set; }

        public static IEnumerable<GeneratedEventMetadata> Parse(string contractName, string @namespace, SolcNet.DataDescription.Output.Contract contract)
        {
            foreach (var item in contract.Abi)
            {
                if (item.Type == AbiType.Event)
                {
                    string eventSignatureHash = AbiSignature.GetSignatureHash(item);
                    yield return new GeneratedEventMetadata
                    {
                        EventSignatureHash = eventSignatureHash,
                        EventClrTypeName = $"{@namespace}.{contractName}.{item.Name}",
                        IndexedArgsCounts = item.Inputs.Count(a => a.Indexed.GetValueOrDefault())
                    };
                }
            }
        }

        /// <summary>
        /// Concats all the event metadata signatures and type names and hashes them.
        /// </summary>
        public static byte[] GetHash(List<GeneratedEventMetadata> items)
        {
            int bufferSize = 0;
            for (var i = 0; i < items.Count; i++)
            {
                bufferSize += items[i].EventClrTypeName.Length + items[i].EventSignatureHash.Length + 2;
            }
            
            Span<char> inputData = stackalloc char[bufferSize];
            Span<char> inputDataCursor = inputData;
            for (var i = 0; i < items.Count; i++)
            {
                items[i].EventClrTypeName.AsSpan().CopyTo(inputDataCursor);
                inputDataCursor = inputDataCursor.Slice(items[i].EventClrTypeName.Length);
                items[i].EventSignatureHash.AsSpan().CopyTo(inputDataCursor);
                inputDataCursor = inputDataCursor.Slice(items[i].EventSignatureHash.Length);
                items[i].IndexedArgsCounts.ToString("00", CultureInfo.InvariantCulture).AsSpan().CopyTo(inputDataCursor);
                inputDataCursor = inputDataCursor.Slice(2);
            }

            var hashBuffer = new byte[32];
            KeccakHash.ComputeHash(MemoryMarshal.AsBytes(inputData), hashBuffer);

            return hashBuffer;
        }


    }


}
