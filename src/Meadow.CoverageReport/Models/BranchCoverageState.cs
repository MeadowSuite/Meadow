using System;
using System.Runtime.Serialization;

namespace Meadow.CoverageReport.Models
{
    [Flags]
    public enum BranchCoverageState : int
    {
        [EnumMember(Value = "none")]
        CoveredNone = 0,

        [EnumMember(Value = "if")]
        CoveredIf = (1 << 0),

        [EnumMember(Value = "else")]
        CoveredElse = (1 << 1),

        [EnumMember(Value = "both")]
        CoveredBoth = CoveredIf | CoveredElse
    }
}
