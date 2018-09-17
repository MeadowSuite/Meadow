using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Meadow.CoverageReport.Models
{
    public class SourceFileLine
    {
        public bool IsActive { get; set; }
        public bool IsUnreachable { get; set; }
        public int LineNumber { get; set; }
        public string LiteralSourceCodeLine { get; set; }
        public int ExecutionCount { get; set; }
        public bool IsCovered => ExecutionCount > 0;

        [JsonConverter(typeof(StringEnumConverter))]
        public BranchCoverageState BranchState { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BranchType? BranchType { get; set; }

        public bool IsBranch { get; set; }

        [JsonIgnore]
        public SourceFileMap SourceFileMapParent { get; set; }

        public int Offset { get; set; }
        public int OffsetEnd => Offset + Length;
        public int Length { get; set; }

        [JsonIgnore]
        public AstNode[] CorrelatedAstNodes { get; set; }
    }
}
