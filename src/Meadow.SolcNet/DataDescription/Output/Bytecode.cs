using Newtonsoft.Json;
using SolcNet.DataDescription.Parsing;
using System;
using System.Collections.Generic;

namespace SolcNet.DataDescription.Output
{
    public class Bytecode
    {
        /// <summary>
        /// The bytecode as a hex string.
        /// </summary>
        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonIgnore]
        byte[] _objectBytes;

        [JsonIgnore]
        public byte[] ObjectBytes => _objectBytes ?? (_objectBytes = HexUtil.HexToBytes(Object));

        /// <summary>
        /// Opcodes list (string)
        /// </summary>
        [JsonProperty("opcodes")]
        public string Opcodes { get; set; }

        /// <summary>
        /// The source mapping as a string. See the source mapping definition.
        /// <see href="http://solidity.readthedocs.io/en/v0.4.24/miscellaneous.html#source-mappings"/>
        /// </summary>
        [JsonProperty("sourceMap"), JsonConverter(typeof(SourceMapJsonConverter))]
        public SourceMaps SourceMap { get; set; }

        /// <summary>
        /// If given, this is an unlinked object.
        /// </summary>
        [JsonProperty("linkReferences")]
        public Dictionary<string /*sol file*/, Dictionary<string/*contract name*/, LinkReference[]>> LinkReferences { get; set; }
    }

    /// <summary>
    /// Initially only holds the encoded source map string in <see cref="EncodedValue"/>.
    /// The source map entries are lazily parsed when <see cref="Entries"/> is first accessed.
    /// </summary>
    public class SourceMaps
    {
        /// <summary>
        /// The encoded source map string.
        /// <see href="http://solidity.readthedocs.io/en/v0.4.24/miscellaneous.html#source-mappings"/>
        /// </summary>
        public string EncodedValue { get; set; }

        SourceMapEntry[] _entries;

        /// <summary>
        /// Each of these elements corresponds to an instruction, i.e. you cannot use the byte 
        /// offset but have to use the instruction offset (push instructions are longer than a 
        /// single byte).
        /// Lazily parses the <see cref="EncodedValue"/> when first accessed.
        /// </summary>
        public SourceMapEntry[] Entries => _entries ?? (_entries = EncodedValue == null ? null : SourceMapParser.Parse(EncodedValue));
    }

    /// <summary>
    /// The parsed source map data.
    /// <see href="http://solidity.readthedocs.io/en/v0.4.24/miscellaneous.html#source-mappings"/>
    /// </summary>
    public struct SourceMapEntry
    {
        /*
        Where s is the byte-offset to the start of the range in the source file, 
        l is the length of the source range in bytes and f is the source index 
        mentioned above. The encoding in the source mapping for the bytecode is 
        more complicated: It is a list of s:l:f:j separated by ;. Each of these 
        elements corresponds to an instruction, i.e. you cannot use the byte offset 
        but have to use the instruction offset (push instructions are longer than 
        a single byte). The fields s, l and f are as above and j can be either i, o 
        or - signifying whether a jump instruction goes into a function, returns 
        from a function or is a regular jump as part of e.g. a loop. In order to 
        compress these source mappings especially for bytecode, the following rules 
        are used:
            If a field is empty, the value of the preceding element is used.
            If a : is missing, all following fields are considered empty.
        */

        /// <summary>The byte-offset to the start of the range in the source file.</summary>
        public int Offset;

        /// <summary>The length of the source range in bytes.</summary>
        public int Length;

        /// <summary>
        /// Integer indentifier to refer to source file. In the case of instructions that are not 
        /// associated with any particular source file, the source mapping assigns an integer 
        /// identifier of -1. This may happen for bytecode sections stemming from compiler-generated 
        /// inline assembly statements.
        /// </summary>
        public int Index;

        /// <summary>
        /// Signifies whether a jump instruction goes into a function, returns from a function or 
        /// is a regular jump as part of e.g. a loop.
        /// </summary>
        public JumpInstruction Jump;
    }

    public enum JumpInstruction : byte
    {
        /// <summary>jump instruction goes into a function</summary>
        Function = (byte)'i',
        /// <summary>returns from a function</summary>
        Return = (byte)'o',
        /// <summary>regular jump as part of e.g. a loop</summary>
        Regular = (byte)'-'
    }

    public class LinkReference
    {
        /// <summary>
        /// Byte offsets into the bytecode. Linking replaces the 20 bytes located there.
        /// </summary>
        [JsonProperty("start", Required = Required.Always)]
        public uint Start { get; set; }

        [JsonProperty("length", Required = Required.Always)]
        public uint Length { get; set; }
    }

}
