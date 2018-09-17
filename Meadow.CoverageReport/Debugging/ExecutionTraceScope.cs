using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging
{
    /// <summary>
    /// Represents a scope in an exection trace (the set of instructions that signify executing in one function context or another).
    /// </summary>
    public class ExecutionTraceScope
    {
        #region Properties
        /// <summary>
        /// The contract definition for the contract this scope's execution context takes place in.
        /// </summary>
        public AstContractDefinition ContractDefinition { get; set; }
        /// <summary>
        /// The trace index where the function definition is first resolved for this scope.
        /// </summary>
        public int? FunctionDefinitionIndex { get; set; }
        /// <summary>
        /// The function definition for the function this scope's execution context takes place in.
        /// </summary>
        public AstFunctionDefinition FunctionDefinition { get; set; }
        /// <summary>
        /// The trace index into the execution trace where the first instruction for this scope occurred.
        /// </summary>
        public int StartIndex { get; set; }
        /// <summary>
        /// The trace index into the execution trace where the last instruction for this scope occurred.
        /// </summary>
        public int EndIndex { get; set; }
        /// <summary>
        /// The depth starting from zero which signifies how many external function calls/EVM executes deep the current scope takes place in.
        /// </summary>
        public int CallDepth { get; set; }
        /// <summary>
        /// Signifies how many scopes are ancestors to this scope.
        /// </summary>
        public int ScopeDepth { get; set; }
        /// <summary>
        /// The parent scope which contains this scope, if any.
        /// </summary>
        public ExecutionTraceScope Parent { get; set; }
        /// <summary>
        /// Represents the ast node for the function call which had created/invoked this scope.
        /// </summary>
        public AstNode ParentFunctionCall { get; set; }

        /// <summary>
        /// Represents a lookup of local variables by ID.
        /// </summary>
        public Dictionary<long, LocalVariable> Locals { get; }
        #endregion

        #region Constructors
        public ExecutionTraceScope(int startIndex, int scopeDepth, int callDepth, ExecutionTraceScope parentScope = null)
        {
            // Set our properties
            StartIndex = startIndex;
            ScopeDepth = scopeDepth;
            CallDepth = callDepth;
            Parent = parentScope;

            // Set our definition properties
            ContractDefinition = null;
            FunctionDefinitionIndex = null;
            FunctionDefinition = null;
            ParentFunctionCall = null;

            // Initialize our locals list
            Locals = new Dictionary<long, LocalVariable>();
        }
        #endregion

        #region Functions
        /// <summary>
        /// Adds a local variable to this execution scopes lookup, if one with this ID was not already resolved.
        /// </summary>
        /// <param name="localVariable">The local variable to add if it has not been added already.</param>
        public void AddLocalVariable(LocalVariable localVariable)
        {
            // Set our local variable in our dictionary.
            if (!Locals.ContainsKey(localVariable.Declaration.Id))
            {
                Locals[localVariable.Declaration.Id] = localVariable;
            }
        }

        /// <summary>
        /// Sets the function definition for this scope, and the trace index at which it was resolved.
        /// </summary>
        /// <param name="traceIndex">The trace index at which this function definition was resolved.</param>
        /// <param name="functionDefinition">The function definition which this scope executed inside of.</param>
        public void SetFunctionDefinitionAndIndex(int traceIndex, AstFunctionDefinition functionDefinition)
        {
            // Try to set our scope definition properties.
            FunctionDefinitionIndex = traceIndex;
            FunctionDefinition = new AstFunctionDefinition(functionDefinition);
            ContractDefinition = functionDefinition?.GetImmediateOrAncestor<AstContractDefinition>();
        }
        #endregion
    }
}
