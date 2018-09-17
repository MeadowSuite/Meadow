using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Types;
using Meadow.JsonRpc.Types.Debugging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport
{
    static class CoverageOpcodeMapping
    {

        static ConcurrentDictionary<string, Dictionary<int, int>> _instructionOffsetNumberCache = new ConcurrentDictionary<string, Dictionary<int, int>>();

        public static Dictionary<int, int> GetInstructionNumberToOffsetLookup(string opcodes)
        {
            // Obtain our offset to number dictionary
            var offsetToNumbers = GetInstructionOffsetToNumberLookup(opcodes).ToDictionary(k => k.Value, k => k.Key);
            return offsetToNumbers;
        }

        public static Dictionary<int, int> GetInstructionOffsetToNumberLookup(string opcodes)
        {
            if (_instructionOffsetNumberCache.TryGetValue(opcodes, out var result))
            {
                return result;
            }

            // Obtain our byte code data
            string[] opcodeItems = opcodes.Split(' ');

            // Next we'll want to loop for every item in this list to map instruction indexes to offsets.
            Dictionary<int, int> instructionOffsetToNumber = new Dictionary<int, int>();
            int instructionIndex = 0;
            int instructionOffset = 0;
            foreach (string opcodeItem in opcodeItems)
            {
                if (opcodeItem.Length < 2 || opcodeItem.Substring(0, 2) != "0x")
                {
                    // This is an opcode, so we set the lookup index.
                    instructionOffsetToNumber[instructionOffset] = instructionIndex;

                    int instructionSize = 1;
                    if (opcodeItem.Length > 4 && opcodeItem.Substring(0, 4) == "PUSH")
                    {
                        instructionSize = 1 + int.Parse(opcodeItem.Substring(4), CultureInfo.InvariantCulture);
                    }

                    // Increment our offset and index
                    instructionIndex++;
                    instructionOffset += instructionSize;
                }
                else
                {
                    // This is data, we already skipped the size for this, so we stop.
                }
            }

            _instructionOffsetNumberCache.TryAdd(opcodes, instructionOffsetToNumber);

            // Return our lookup.
            return instructionOffsetToNumber;
        }

        public static (Address Address, uint[] InstructionIndexCoverage, int[] JumpIndexes, int[] NonJumpIndexes) ConvertToSolidityCoverageMap(CoverageMap coverageMap, string opcodes)
        {
            // We create a new map for instruction index->execution count instead of instruction offset->execution count. We do the same for jumps
            List<uint> newMap = new List<uint>();
            List<int> newJumps = new List<int>();
            List<int> newNonJumps = new List<int>();


            // Next we'll want to loop for every item in this list to map instruction indexes to offsets.
            Dictionary<int, int> instructionOffsetToNumber = GetInstructionOffsetToNumberLookup(opcodes);

            // We'll want to convert our map from using offsets to indexes, so we use a list and add every consecutive instruction to it.
            for (int i = 0; i < coverageMap.Map.Length; i++)
            {
                // If this offset is in our offset to number translation, there's an instruction at this offset, so we add the execution count.
                if (instructionOffsetToNumber.ContainsKey(i))
                {
                    newMap.Add(coverageMap.Map[i]);
                }
            }

            // Next we'll want to update our jumps
            for (int i = 0; i < coverageMap.JumpOffsets.Length; i++)
            {
                // Obtain our jump offset.
                int offset = coverageMap.JumpOffsets[i];

                // Verify there is an instruction number for this
                if (!instructionOffsetToNumber.ContainsKey(offset))
                {
                    throw new ArgumentException("Could not map instruction offset to instruction index for jump indexes.");
                }

                // Add our jump instruction index to our list.
                newJumps.Add(instructionOffsetToNumber[offset]);
            }

            // Next we'll want to update our non-jumps
            for (int i = 0; i < coverageMap.NonJumpOffsets.Length; i++)
            {
                // Obtain our jump offset.
                int offset = coverageMap.NonJumpOffsets[i];

                // Verify there is an instruction number for this
                if (!instructionOffsetToNumber.ContainsKey(offset))
                {
                    throw new ArgumentException("Could not map instruction offset to instruction index for non-jump indexes.");
                }

                // Add our jump instruction index to our list.
                newNonJumps.Add(instructionOffsetToNumber[offset]);
            }

            // Obtain the result and return it.
            var result = (coverageMap.ContractAddress, newMap.ToArray(), newJumps.ToArray(), newNonJumps.ToArray());
            return result;
        }
    }
}
