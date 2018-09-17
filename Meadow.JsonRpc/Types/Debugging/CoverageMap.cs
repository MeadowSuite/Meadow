using Meadow.Core.EthTypes;
using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.JsonRpc.Types.Debugging
{
    public class CoverageMap
    {
        #region Properties
        /// <summary>
        /// DATA, 20 Bytes - address of the sender.
        /// </summary>
        [JsonProperty("contractAddress"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Address ContractAddress { get; set; }
        /// <summary>
        /// DATA, variable length, same length as code section. The index in the map indicates how many times the instruction at that position in the code was executed.
        /// </summary>
        [JsonProperty("coverageMap"), JsonConverter(typeof(StructArrayHexConverter<uint>))]
        public uint[] Map { get; set; }
        /// <summary>
        /// DATA, variable length, an array of indexes into <see cref="Map"/> which indicate where a jump did occur (jump branch was taken). 
        /// </summary>
        [JsonProperty("jumpOffsets"), JsonConverter(typeof(StructArrayHexConverter<int>))]
        public int[] JumpOffsets { get; set; }
        /// <summary>
        /// DATA, variable length, an array of indexes into <see cref="Map"/> which indicate where a jump did occur (jump branch was taken). 
        /// </summary>
        [JsonProperty("nonJumpOffsets"), JsonConverter(typeof(StructArrayHexConverter<int>))]
        public int[] NonJumpOffsets { get; set; }
        /// <summary>
        /// DATA, the code we're mapping on.
        /// </summary>
        [JsonProperty("code"), JsonConverter(typeof(JsonRpcHexConverter))]
        public byte[] Code { get; set; }
        #endregion
    }
}
