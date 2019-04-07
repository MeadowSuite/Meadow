using SolcNet.DataDescription.Output;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SolcNet.DataDescription.Parsing
{

    public static class SourceMapParser
    {
        public static SourceMapEntry[] Parse(string encodedValue)
        {
            var valParts = encodedValue.Split(';');
            var entries = new SourceMapEntry[valParts.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                // If a : is missing, all following fields are considered empty.
                if (valParts[i] == string.Empty)
                {
                    entries[i] = entries[i - 1];
                }
                else
                {
                    var entryParts = valParts[i].Split(':');

                    entries[i].Offset = entryParts[0] != string.Empty
                        ? int.Parse(entryParts[0], CultureInfo.InvariantCulture)
                        : entries[i - 1].Offset;

                    entries[i].Length = entryParts.Length > 1 && entryParts[1] != string.Empty
                        ? int.Parse(entryParts[1], CultureInfo.InvariantCulture)
                        : entries[i - 1].Length;

                    entries[i].Index = entryParts.Length > 2 && entryParts[2] != string.Empty
                        ? int.Parse(entryParts[2], CultureInfo.InvariantCulture)
                        : entries[i - 1].Index;

                    entries[i].Jump = entryParts.Length > 3 && entryParts[3] != string.Empty
                        ? (JumpInstruction)(byte)entryParts[3][0]
                        : entries[i - 1].Jump;
                }
            }
            return entries;
        }
    }
}
