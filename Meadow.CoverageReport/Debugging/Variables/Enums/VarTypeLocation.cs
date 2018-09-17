using Meadow.CoverageReport.AstTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.Enums
{
    /// <summary>
    /// Represents a <see cref="AstVariableDeclaration"/>'s location as it is referred to by its type descriptors.
    /// </summary>
    public enum VarTypeLocation
    {
        NoneSpecified,
        StorageRef,
        StoragePtr,
        Memory,
        CallData
    }
}
