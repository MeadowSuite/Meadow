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
        public string ClrTypeName { get; set; }
        public string ClrTypeFullName { get; set; }
        public int IndexedArgsCounts { get; set; }
        public Abi AbiItem { get; set; }
        

        public static IEnumerable<GeneratedEventMetadata> Parse(string contractName, string @namespace, SolcNet.DataDescription.Output.Contract contract)
        {
            foreach (var item in contract.Abi)
            {
                if (item.Type == AbiType.Event)
                {
                    // Check if this event name is overloaded (duplicate events with same name but different types).
                    var isOverloaded = contract.Abi
                        .Where(i => i.Type == AbiType.Event)
                        .Where(i => i.Name == item.Name)
                        .Where(i => !ReferenceEquals(i, item))
                        .Any();

                    string eventName;

                    if (isOverloaded)
                    {
                        string GetInputTypeName(string type)
                        {
                            // Format an array type def into name without symbols,
                            // supporting dynamic and fixed sized types such as:
                            // uint32[5][7][9] -> uint32Array5Array7Array9
                            // uint32[5][][9] -> uint32Array5ArrayArray9
                            // TODO: there is probably a regex to do this better.
                            return type
                               .Replace("[]", "Array", StringComparison.Ordinal)
                               .Trim('[', ']')
                               .Replace("[", "Array", StringComparison.Ordinal)
                               .Replace("]", string.Empty, StringComparison.Ordinal);
                        }

                        // If the event is overloaded, append the event data types to the name.
                        eventName = item.Name + "_" + string.Join("_", item.Inputs.Select(i => GetInputTypeName(i.Type)));
                    }
                    else
                    {
                        eventName = ReservedKeywords.EscapeIdentifier(item.Name);
                    }

                    string eventSignatureHash = AbiSignature.GetSignatureHash(item);
                    yield return new GeneratedEventMetadata
                    {
                        AbiItem = item,
                        EventSignatureHash = eventSignatureHash,
                        ClrTypeName = eventName,
                        ClrTypeFullName = $"{@namespace}.{contractName}.{eventName}",
                        IndexedArgsCounts = item.Inputs.Count(a => a.Indexed.GetValueOrDefault()),
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
                bufferSize += items[i].ClrTypeFullName.Length + items[i].EventSignatureHash.Length + 2;
            }
            
            Span<char> inputData = stackalloc char[bufferSize];
            Span<char> inputDataCursor = inputData;
            for (var i = 0; i < items.Count; i++)
            {
                items[i].ClrTypeFullName.AsSpan().CopyTo(inputDataCursor);
                inputDataCursor = inputDataCursor.Slice(items[i].ClrTypeFullName.Length);
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
