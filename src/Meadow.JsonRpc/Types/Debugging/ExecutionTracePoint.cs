using Meadow.Core.EthTypes;
using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.JsonRpc.Types.Debugging
{
    public class ExecutionTracePoint
    {
        #region Properties
        /// <summary>
        /// The call data set at this trace point.
        /// If NULL, then derived from last known value in the trace.
        /// </summary>
        [JsonProperty("callData"), JsonConverter(typeof(JsonRpcHexConverter))]
        public byte[] CallData { get; set; }
        /// <summary>
        /// The full code segment executed at this trace point (including constructor args if deploying, etc).
        /// Hence, this section will treat code segments as the EVM internally does, not discerning higher level structures and splitting them.
        /// If NULL, then derived from last known value in the trace.
        /// </summary>
        [JsonProperty("code"), JsonConverter(typeof(JsonRpcHexConverter))]
        public byte[] Code { get; set; }
        /// <summary>
        /// DATA, 20 Bytes (Optional) - Address of the contract this execution point takes place in. If NULL, then derived from last known value in the trace.
        /// </summary>
        [JsonProperty("contractAddress"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Address? ContractAddress { get; set; }
        /// <summary>
        /// BOOLEAN, indicates whether this execution point was occuring on a deployed contract (true) or a deploying contract (false).
        /// </summary>
        [JsonProperty("contractDeployed")]
        public bool ContractDeployed { get; set; }
        /// <summary>
        /// QUANTITY - The amount of gas remaining for this execution.
        /// </summary>
        [JsonProperty("gas"), JsonConverter(typeof(JsonRpcHexConverter))]
        public UInt256 GasRemaining { get; set; }
        /// <summary>
        /// QUANTITY - The cost of gas for this execution point to complete.
        /// </summary>
        [JsonProperty("gasCost"), JsonConverter(typeof(JsonRpcHexConverter))]
        public UInt256 GasCost { get; set; }
        /// <summary>
        /// DATA, variable length, a string mnemonic for the operation.
        /// </summary>
        [JsonProperty("op")]
        public string Opcode { get; set; }
        /// <summary>
        /// UNSIGNED INTEGER, 32-bit, represents the program counter at this point in execution.
        /// </summary>
        [JsonProperty("pc"), JsonConverter(typeof(JsonRpcHexConverter))]
        public uint PC { get; set; }
        /// <summary>
        /// UNSIGNED INTEGER, 32-bit, represents the message call depth at this point in execution.
        /// </summary>
        [JsonProperty("depth"), JsonConverter(typeof(JsonRpcHexConverter))]
        public uint Depth { get; set; }
        /// <summary>
        /// DATA array, variable length (multiple of 32) - (Optional) Represents the EVM memory at this point in execution. NULL if unchanged since last known value in trace.
        /// </summary>
        [JsonProperty("memory", ItemConverterType = typeof(DataHexJsonConverter))]
        public Data[] Memory { get; set; }
        /// <summary>
        /// DATA array, variable length (multiple of 32) - Represents the EVM stack at this point in execution.
        /// </summary>
        [JsonProperty("stack", ItemConverterType = typeof(DataHexJsonConverter))]
        public Data[] Stack { get; set; }

        [JsonProperty("storage"), JsonConverter(typeof(ByteArrayDictionaryConverter))]
        public Dictionary<Memory<byte>, byte[]> Storage { get; set; }
        #endregion

        #region Functions
        /// <summary>
        /// Obtains a singular unified/contiguous memory representation, combined from all entries in <see cref="Memory"/>.
        /// </summary>
        /// <returns>Returns a signular and contiguous representation of memory.</returns>
        public Memory<byte> GetContiguousMemory()
        {
            // Compute the total linear memory size.
            int memorySize = Memory.Length * Data.SIZE;

            // Define the memory itself, with the appropriate size.
            Memory<byte> memory = new byte[memorySize];

            // Copy all memory into this representation.
            for (int i = 0; i < Memory.Length; i++)
            {
                Memory[i].GetSpan().CopyTo(memory.Span.Slice(i * Data.SIZE, Data.SIZE));
            }

            return memory;
        }
        #endregion
    }
}
