using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Meadow.CoverageReport
{
    [Flags]
    public enum BranchType
    {
        [EnumMember(Value = "none")]
        None,

        [EnumMember(Value = "if_statement")]
        IfStatement,

        [EnumMember(Value = "ternary")]
        Ternary,

        [EnumMember(Value = "assert")]
        Assert,

        [EnumMember(Value = "require")]
        Require
    }
}
