using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.DebugAdapterServer
{
    public enum DesiredControlFlow
    {
        Continue,
        StepInto,
        StepOver,
        StepBackwards,
        StepOut
    }
}
