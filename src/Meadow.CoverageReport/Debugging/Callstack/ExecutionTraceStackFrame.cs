using Meadow.CoverageReport.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Callstack
{
    public struct ExecutionTraceStackFrame
    {
        #region Properties
        /// <summary>
        /// Indicates the current lines executing in this stack frame have been resolved.
        /// </summary>
        public bool Error { get; private set; }

        /// <summary>
        /// Indicates this stack frame has been resolved to code within a function definition.
        /// </summary>
        public bool ResolvedFunction
        {
            get
            {
                return Scope?.FunctionDefinition != null;
            }
        }

        /// <summary>
        /// Indicates whether the stack frame was mapped to a function, and whether the function is a constructor.
        /// </summary>
        public bool IsFunctionConstructor
        {
            get
            {
                return Scope?.FunctionDefinition?.IsConstructor == true;
            }
        }

        /// <summary>
        /// Indicates the name of the function. If this is a constructor without a name, it takes the name of its encapsulating contract.
        /// </summary>
        public string FunctionName
        {
            get
            {
                return IsFunctionConstructor ? Scope?.ContractDefinition?.Name : Scope?.FunctionDefinition?.Name;
            }
        }
        #endregion

        #region Fields
        /// <summary>
        /// The execution trace scope which defines the the scope this stack frame is currently executing in.
        /// </summary>
        public readonly ExecutionTraceScope Scope;

        /// <summary>
        /// The source files lines which indicate the current position in the stack frame.
        /// </summary>
        public readonly SourceFileLine[] CurrentPositionLines;

        /// <summary>
        /// Indicates a trace index which represents the current source position (<see cref="CurrentPositionLines"/>))
        /// in this stack frame's scope.
        /// </summary>
        public readonly int CurrentPositionTraceIndex;
        #endregion

        #region Constructor
        public ExecutionTraceStackFrame(ExecutionTraceScope scope, SourceFileLine[] currentLines, int lastTraceIndex, bool error)
        {
            // Set our properties
            Scope = scope;
            CurrentPositionLines = currentLines ?? Array.Empty<SourceFileLine>();
            CurrentPositionTraceIndex = lastTraceIndex;
            Error = error;
        }
        #endregion
    }
}
