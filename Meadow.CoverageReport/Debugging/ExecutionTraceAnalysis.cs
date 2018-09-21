using Meadow.Contract;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables;
using Meadow.CoverageReport.Debugging.Variables.Storage;
using Meadow.CoverageReport.Models;
using Meadow.JsonRpc.Types.Debugging;
using SolcNet.DataDescription.Output;
using SolcNet.DataDescription.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport.Debugging
{
    /// <summary>
    /// Analyzes an execution trace to perform high level code analysis on the instruction execution using the respective source maps/abstract syntax trees.
    /// </summary>
    public class ExecutionTraceAnalysis
    {
        #region Fields
        /// <summary>
        /// An array of code hashes for every trace point.
        /// </summary>
        private string[] _cachedTracepointCodes;

        /// <summary>
        /// A cache of contract states (address+deployed) to contract maps (source maps and instruction offset to number lookup).
        /// </summary>
        private Dictionary<(Address ContractAddress, string CodeHash, bool Deployed), (SourceMapEntry[] SourceMap, Dictionary<int, int> InstructionOffsetToNumber)> _contractMapCache;

        /// <summary>
        /// Cache for the generated solc data from the contracts in the project.
        /// </summary>
        (SolcSourceInfo[] SolcSourceInfo, SolcBytecodeInfo[] SolcBytecodeInfo) _solcData;

        /// <summary>
        /// A collection of all state VariableDeclaration AST nodes.
        /// </summary>
        private AstNode[] _astStateVariableDeclarations;
        #endregion

        #region Properties
        /// <summary>
        /// The execution trace which provides information regarding every instruction executed.
        /// </summary>
        public ExecutionTrace ExecutionTrace { get; }
        /// <summary>
        /// A lookup of scopes using the start trace index for that scope. It is important to note
        /// that these scopes seperate internal and external calls, but child scopes will also be contained in the parent scopes, since we are likely to return to them.
        /// </summary>
        public Dictionary<int, ExecutionTraceScope> Scopes { get; }
        /// <summary>
        /// Handles storage slot index/data offset resolving as well as stores information about the current trace index context to resolve local/state variables.
        /// </summary>
        public StorageManager StorageManager { get; }
        /// <summary>
        /// Indices which signify the index of each significant step in the trace, where a significant step is defined as one that advances in source position from the previous.
        /// </summary>
        public ReadOnlyCollection<int> SignificantStepIndices { get; private set; }
        #endregion

        #region Constructor
        public ExecutionTraceAnalysis(ExecutionTrace executionTrace)
        {
            // Set our execution trace
            ExecutionTrace = executionTrace;

            // Initialize our storage manager
            StorageManager = new StorageManager(ExecutionTrace);

            // Initialize our contract cache
            _contractMapCache = new Dictionary<(Address ContractAddress, string CodeHash, bool Deployed), (SourceMapEntry[] SourceMap, Dictionary<int, int> InstructionOffsetToNumber)>();

            // Initialize our lookup for scopes.
            Scopes = new Dictionary<int, ExecutionTraceScope>();

            // Fill in our null/unchanged values for our execution trace to make it easier to parse.
            FillUnchangedValues();

            // Parse all the ast nodes.
            AstParser.Parse();

            // Set our solc data
            _solcData = GeneratedSolcData.Default.GetSolcData();

            // Parse our state variable declarations.
            ParseStateVariableDeclarations();

            // Parse our entire execution trace.
            ParseScopes(0, 0, 0, null);
        }
        #endregion

        #region TODO: Move elsewhere and cleanup
        public SourceFileLine[] GetSourceLines()
        {
            // Obtain our lines for the current point in execution.
            return GetSourceLines(ExecutionTrace.Tracepoints.Length - 1);
        }

        public SourceFileLine[] GetSourceLines(int traceIndex)
        {
            // Loop backwards from our trace index
            for (int i = traceIndex; i >= 0; i--)
            {
                // Obtain our source map entry and instruction number for this point inthe execution trace.
                (int instructionIndex, SourceMapEntry sourceMapEntry) = GetInstructionAndSourceMap(i);

                // Obtain our analysis
                var analysis = SourceAnalysis.Run(_solcData.SolcSourceInfo, _solcData.SolcBytecodeInfo);

                // Obtain our source file maps and lines.
                var sourceFileMaps = ReportGenerator.CreateSourceFileMaps(_solcData.SolcSourceInfo, analysis);

                // Obtain the lines corresponding to this source map entry.
                var lines = SourceLineMatching.GetSourceFileLinesFromSourceMapEntry(sourceMapEntry, sourceFileMaps);

                // If our lines could not be obtained, skip to the previous trace index
                if (lines == null)
                {
                    continue;
                }

                // Return all obtained lines.
                return lines.ToArray();
            }

            // Throw our exception.
            throw new Exception("Could not resolve source lines for the given trace index. Attempt to walk backwards to the last resolvable lines have also failed. Please report this.");
        }

        private SourceFileLine[] GetSourceLines(AstNode node)
        {
            // Obtain our analysis
            var analysis = SourceAnalysis.Run(_solcData.SolcSourceInfo, _solcData.SolcBytecodeInfo);

            // Obtain our source file maps and lines.
            var sourceFileMaps = ReportGenerator.CreateSourceFileMaps(_solcData.SolcSourceInfo, analysis);

            // Obtain our source lines and return them.
            SourceFileLine[] lines = SourceLineMatching.GetSourceFileLinesContainingAstNode(node, sourceFileMaps).OrderBy(k => k.Offset).ToArray();

            // Return all obtained lines.
            return lines;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Fills in values that are marked as unchanged from the last known values (to save memory in deserialization).
        /// Unchanges values are reference types, and setting unchanged values to last known here will have no real memory cost.
        /// </summary>
        private void FillUnchangedValues()
        {
            // Initialize our code hash array
            _cachedTracepointCodes = new string[ExecutionTrace.Tracepoints.Length];

            // Declare some variables to track state.
            Core.EthTypes.Data[] lastKnownStack = null;
            Core.EthTypes.Data[] lastKnownMemory = null;
            Address? lastKnownContractAddress = null;
            byte[] lastKnownCode = null;
            string lastKnownCodeHex = null;
            Dictionary<Memory<byte>, byte[]> lastKnownStorage = null;

            // Loop for each trace point.
            for (int i = 0; i < ExecutionTrace.Tracepoints.Length; i++)
            {
                // Obtain our tracepoint
                ExecutionTracePoint tracePoint = ExecutionTrace.Tracepoints[i];

                // Update any "unchanged" (null) references to the last known reference for memory
                if (tracePoint.Memory == null)
                {
                    tracePoint.Memory = lastKnownMemory;
                }
                else
                {
                    lastKnownMemory = tracePoint.Memory;
                }

                // Update any "unchanged" (null) references to the last known reference for stack
                if (tracePoint.Stack == null)
                {
                    tracePoint.Stack = lastKnownStack;
                }
                else
                {
                    lastKnownStack = tracePoint.Stack;
                }

                // Update any "unchanged" (null) references to the last known reference for contract address
                if (tracePoint.ContractAddress == null)
                {
                    tracePoint.ContractAddress = lastKnownContractAddress;
                }
                else
                {
                    lastKnownContractAddress = (Address)tracePoint.ContractAddress;
                }

                // Update any "unchanged" (null) references to the last known reference for code
                if (tracePoint.Code == null)
                {
                    tracePoint.Code = lastKnownCode;
                }
                else
                {
                    lastKnownCode = tracePoint.Code;
                    lastKnownCodeHex = lastKnownCode.ToHexString(hexPrefix: false);
                }

                // Set our code hash
                _cachedTracepointCodes[i] = lastKnownCodeHex;

                // Update any "unchanged" (null) references to the last known reference for storage
                if (tracePoint.Storage == null)
                {
                    tracePoint.Storage = lastKnownStorage;
                }
                else
                {
                    lastKnownStorage = tracePoint.Storage;
                }
            }
        }

        /// <summary>
        /// Obtains the instruction index and source map entry for a given trace point index in our execution trace.
        /// </summary>
        /// <param name="traceIndex">The index of the trace point in our execution trace for which we want to obtain the current instruction and source map entry.</param>
        /// <returns>Returns the instruction index and source map entry for a given trace point index in our execution trace.</returns>
        public (int InstructionIndex, SourceMapEntry SourceMapEntry) GetInstructionAndSourceMap(int traceIndex)
        {
            // Obtain our trace point
            ExecutionTracePoint tracepoint = ExecutionTrace.Tracepoints[traceIndex];

            // Next we'll want to obtain the contract map for this contract. We check cache first.
            (SourceMapEntry[] SourceMap, Dictionary<int, int> InstructionOffsetToNumber) contractMap;

            var codeHex = _cachedTracepointCodes[traceIndex];
            var cacheKey = (tracepoint.ContractAddress.Value, codeHex, tracepoint.ContractDeployed);
            if (_contractMapCache.ContainsKey(cacheKey))
            {
                // Set our contract map from our cache.
                contractMap = _contractMapCache[cacheKey];
            }
            else
            {
                SolcBytecodeInfo info;

                // Try to obtain the contract
                if (tracepoint.ContractDeployed)
                {
                    codeHex = tracepoint.Code.ToHexString(hexPrefix: false);
                    if (!GeneratedSolcData.Default.GetSolcBytecodeInfoByCodeMatch(codeHex, isDeployed: true, out info))
                    {
                        throw new Exception($"Could not match trace analysis deployed code to solc outputs {codeHex}");
                    }
                }
                else
                {
                    codeHex = tracepoint.Code.ToHexString(hexPrefix: false);
                    if (!GeneratedSolcData.Default.GetSolcBytecodeInfoByCodeMatch(codeHex, isDeployed: false, out info))
                    {
                        throw new Exception($"Could not match trace analysis undeployed code to solc outputs {codeHex}");
                    }
                }

                // Find the solc output for this contract address
                var bytecodeInfo = _solcData.SolcBytecodeInfo.Single(s => s.FilePath == info.FilePath && s.ContractName == info.ContractName && s.BytecodeDeployedHash == info.BytecodeDeployedHash);

                // Parse the opcode string to do offset to index number lookups
                var opcodes = tracepoint.ContractDeployed ? bytecodeInfo.OpcodesDeployed : bytecodeInfo.Opcodes;
                var instructionOffsetToNumber = CoverageOpcodeMapping.GetInstructionOffsetToNumberLookup(opcodes);

                // Grab source map entry for this instruction number.
                var sourceMapString = tracepoint.ContractDeployed ? bytecodeInfo.SourceMapDeployed : bytecodeInfo.SourceMap;
                var sourceMap = SourceMapParser.Parse(sourceMapString);

                // Set our contract map
                contractMap = (sourceMap, instructionOffsetToNumber);

                // Set the contract map in cache as well
                _contractMapCache[cacheKey] = contractMap;
            }

            // Grab the instruction number for this trace index
            int instructionNumber = contractMap.InstructionOffsetToNumber[(int)tracepoint.PC];

            // Obtain the source map entry for this instruction number.
            SourceMapEntry sourceMapEntry = contractMap.SourceMap[instructionNumber];

            // Return our values.
            return (instructionNumber, sourceMapEntry);
        }

        /// <summary>
        /// Determines if the given trace index has an instruction that will increase message depth/call external code.
        /// </summary>
        /// <param name="traceIndex">The index of the trace point which we want to check to see is a potential call.</param>
        /// <returns>Returns a boolean indicating whether the instruction at the given trace index is a calling instruction of some sort.</returns>
        private bool IsExternalCall(int traceIndex)
        {
            // Obtain the trace point.
            var tracePoint = ExecutionTrace.Tracepoints[traceIndex];

            // Check the opcode
            return
                tracePoint.Opcode == "CALL" ||
                tracePoint.Opcode == "DELEGATECALL" ||
                tracePoint.Opcode == "STATICCALL" ||
                tracePoint.Opcode == "CALLCODE" ||
                tracePoint.Opcode == "CREATE";
        }

        /// <summary>
        /// Determines if the given trace index has an instruction that is a jump destination.
        /// </summary>
        /// <param name="traceIndex">The index of the trace point which we want to check to see is a potential jump destination.</param>
        /// <returns>Returns a boolean indicating whether the instruction at the given trace index is a jump destination instruction.</returns>
        private bool IsJumpDestination(int traceIndex)
        {
            // If this index is out of bounds, return false
            if (traceIndex < 0 || traceIndex >= ExecutionTrace.Tracepoints.Length)
            {
                return false;
            }

            // Obtain the trace point.
            var tracePoint = ExecutionTrace.Tracepoints[traceIndex];

            // Check the opcode
            return tracePoint.Opcode == "JUMPDEST";
        }

        /// <summary>
        /// Obtains all execution trace scopes, starting from the current scope, ending with the entry point.
        /// </summary>
        /// <returns>Returns an array of execution trace scopes, starting from most recent scopes entered, to earliest.</returns>
        public ExecutionTraceScope[] GetCallStack()
        {
            // Obtain the callstack for our last point in our trace.
            return GetCallStack(ExecutionTrace.Tracepoints.Length - 1);
        }

        /// <summary>
        /// Obtains all execution trace scopes, starting from the latest scope for the provided trace index, ending with the entry point.
        /// </summary>
        /// <param name="traceIndex">The trace point index which we'd like to obtain the callstack for.</param>
        /// <returns>Returns an array of execution trace scopes, starting from most recent scopes entered, to earliest.</returns>
        public ExecutionTraceScope[] GetCallStack(int traceIndex)
        {
            // Verify our bounds, if we fail, return a blank scope array.
            if (traceIndex < 0 || traceIndex >= ExecutionTrace.Tracepoints.Length)
            {
                return Array.Empty<ExecutionTraceScope>();
            }

            // Obtain our latest scope
            ExecutionTraceScope latestScope = GetScope(traceIndex);
            if (latestScope == null)
            {
                return Array.Empty<ExecutionTraceScope>();
            }

            // Create our array of trace scopes
            ExecutionTraceScope[] callScopes = new ExecutionTraceScope[latestScope.ScopeDepth + 1];
            callScopes[0] = latestScope;

            // Loop for each scope we have to populate
            for (int i = 1; i < callScopes.Length; i++)
            {
                // Set our scope as our previous scope's parent.
                callScopes[i] = callScopes[i - 1].Parent;
            }

            // Return our scopes
            return callScopes;
        }

        /// <summary>
        /// Obtains the scope for the given trace index.
        /// </summary>
        /// <param name="traceIndex">The index in our trace for which we'd like to obtain the scope for.</param>
        /// <returns>Returns a scope instance representing the scope at the given trace point index, or null if one could not be found.</returns>
        public ExecutionTraceScope GetScope(int traceIndex)
        {
            // We'll want to find the starting scope index that is the closest to our trace index, and starts before it.
            int? result = null;
            foreach (var scope in Scopes.Values)
            {
                // If this scope starts after this trace index, skip to the next.
                if (scope.StartIndex > traceIndex || scope.EndIndex < traceIndex)
                {
                    continue;
                }

                // If our result isn't set, or our result is an earlier scope, we update it
                if (!result.HasValue || result.Value < scope.StartIndex)
                {
                    result = scope.StartIndex;
                }
            }

            // If we have a starting index, obtain the scope and return it, otherwise
            if (result.HasValue)
            {
                return Scopes[result.Value];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Generates a message from an execution trace exception.
        /// </summary>
        /// <param name="traceException">The execution trace exception to generate a message for.</param>
        /// <returns>Returns an exception message generated from an execution trace exception.</returns>
        private string GetExceptionMessage(ExecutionTraceException traceException)
        {
            // Define our unresolved label
            string unresolved = "<unresolved>";

            // Add the exception message
            var message = new StringBuilder();
            message.AppendLine(traceException.Message);

            // If we have a trace index, we have a callstack, we add that to the message.
            if (traceException.TraceIndex.HasValue)
            {
                // Obtain our callstack and add every frame to it.
                var callstack = GetCallStack(traceException.TraceIndex.Value);
                for (int i = 0; i < callstack.Length; i++)
                {
                    // Grab our current call frame
                    var currentCallFrame = callstack[i];

                    // We obtain our relevant source lines for this call stack frame.
                    SourceFileLine[] lines = null;

                    // If it's the most recent call, we obtain the line for the current trace.
                    if (i == 0)
                    {
                        lines = GetSourceLines(traceException.TraceIndex.Value);
                    }
                    else
                    {
                        var previousCallFrame = callstack[i - 1];
                        if (previousCallFrame.ParentFunctionCall != null)
                        {
                            lines = GetSourceLines(previousCallFrame.ParentFunctionCall);
                        }
                        else
                        {
                            message.AppendLine("-> <an error occurred preventing callstack resolving for this frame>");
                            continue;
                        }
                    }

                    // Obtain method descriptor information if possible
                    string methodDescriptor = null;
                    if (currentCallFrame.FunctionDefinition?.IsConstructor == true)
                    {
                        methodDescriptor = $"'{currentCallFrame.ContractDefinition?.Name ?? unresolved}' constructor";
                    }
                    else
                    {
                        methodDescriptor = $"method '{currentCallFrame.FunctionDefinition?.Name ?? unresolved}'";
                    }


                    // Obtain our line information if possible.
                    string lineFirstLine = "<unresolved code line>";
                    long lineNumber = -1;
                    string lineFileName = "<unresolved filename>";
                    if (lines.Length > 0)
                    {
                        lineFirstLine = lines[0].LiteralSourceCodeLine.Trim();
                        lineNumber = lines[0].LineNumber;
                        lineFileName = lines[0].SourceFileMapParent.SourceFilePath;
                    }

                    // Else, we obtain the line for the previous' call.
                    message.AppendLine($"-> at '{lineFirstLine}' in {methodDescriptor} in file '{lineFileName}' : line {lineNumber}");
                }
            }

            // Trim the end of our message.
            // Return our message.
            return message.ToString().TrimEnd();
        }

        /// <summary>
        /// Obtains an aggregated exception composed of the collection of exceptions that occurred in <see cref="ExecutionTrace"/>.
        /// </summary>
        /// <returns>Returns an exception that is composed of all exceptions that occurred in <see cref="ExecutionTrace"/>, otherwise null if no exceptions occurred.</returns>
        public ContractExecutionException GetAggregateException(Exception innerException = null)
        {
            // If we have no exceptions, return null.
            if (ExecutionTrace.Exceptions.Length == 0)
            {
                return null;
            }

            // Define our aggregate message
            string message;

            if (ExecutionTrace.Exceptions.Length == 1)
            {
                message = GetExceptionMessage(ExecutionTrace.Exceptions[0]);
            }
            else
            {
                var msgBuilder = new StringBuilder();
                // Set our first line as a precursor to the following multiple messages
                msgBuilder.AppendLine("Multiple exceptions collected during execution:");

                // Define a seperator.
                string seperator = "--------------------------------------------";

                // Loop for each exception in our execution trace.
                for (int i = 0; i < ExecutionTrace.Exceptions.Length; i++)
                {
                    if (i == 0)
                    {
                        msgBuilder.AppendLine(seperator);
                    }

                    // Add our exception number to this.
                    msgBuilder.AppendLine($"Exception {i + 1}:");
                    msgBuilder.AppendLine();

                    // Add it to our aggregate exception message
                    msgBuilder.AppendLine(GetExceptionMessage(ExecutionTrace.Exceptions[i]));
                    msgBuilder.AppendLine(seperator);
                }

                message = msgBuilder.ToString();
            }

            if (innerException != null)
            {
                return new ContractExecutionException(message, innerException);
            }

            return new ContractExecutionException(message);
        }

        /// <summary>
        /// Parse all contract definitions and subsequent state variable declarations.
        /// </summary>
        private void ParseStateVariableDeclarations()
        {
            // Create a list of ast nodes we collect which are state variables
            List<AstNode> stateVariables = new List<AstNode>();

            // Loop for every contract definition
            foreach (var contractDefinitionNode in AstParser.ContractNodes)
            {
                // Loop for every child
                stateVariables.AddRange(contractDefinitionNode.VariableDeclarations);
            }

            // Set our resulting array from our list.
            _astStateVariableDeclarations = stateVariables.ToArray();
        }

        /// <summary>
        /// Parses a single tracepoint in <see cref="ExecutionTrace"/> which
        /// (This function is a helper to <see cref="ParseScopes(int, int, int, ExecutionTraceScope)"/> which handles parsing at the same level, not entering or executing scope.
        /// </summary>
        /// <param name="traceIndex"></param>
        /// <param name="currentEntry"></param>
        /// <param name="currentScope"></param>
        private void ParseScopeTracepoint(int traceIndex, SourceMapEntry currentEntry, ExecutionTraceScope currentScope)
        {
            // Obtain our trace point for this index.
            var tracepoint = ExecutionTrace.Tracepoints[traceIndex];

            // Verify we haven't set a function definition trace index yet.
            if (!currentScope.FunctionDefinitionIndex.HasValue)
            {
                // Find any nodes exactly at this location (which are not the function definition node itself).
                var currentLocationNodes = AstParser.AllAstNodes.Where(a =>
                {
                    return
                        a.NodeType != AstNodeType.FunctionDefinition &&
                        a.SourceRange.SourceIndex == currentEntry.Index &&
                        a.SourceRange.Offset == currentEntry.Offset &&
                        a.SourceRange.Offset + a.SourceRange.Length == currentEntry.Offset + currentEntry.Length;
                });

                // Try to find a function definition parent for any ast node that represents our current position.
                // We do this instead of matching function definition directly to ensure we have entered the function scope.
                AstFunctionDefinition functionDefinitionCandidate = null;
                foreach (var currentLocationNode in currentLocationNodes)
                {
                    // Try to obtain a function definition candidate from the current indexed node.
                    functionDefinitionCandidate = currentLocationNode.GetImmediateOrAncestor<AstFunctionDefinition>();

                    // If we found a node candidate, break out
                    if (functionDefinitionCandidate != null)
                    {
                        break;
                    }
                }

                // Verify we have obtained a function definition candidate.
                if (functionDefinitionCandidate != null && (functionDefinitionCandidate.IsConstructor || IsJumpDestination(traceIndex - 1) || IsExternalCall(traceIndex - 1)))
                {
                    // Set our scope properties
                    currentScope.SetFunctionDefinitionAndIndex(traceIndex, functionDefinitionCandidate);

                    // Create locals for our input parameters.
                    int inputParameterCount = currentScope.FunctionDefinition.Parameters.Length;
                    for (int parameterIndex = 0; parameterIndex < inputParameterCount; parameterIndex++)
                    {
                        // Obtain our parameter
                        AstVariableDeclaration parameter = currentScope.FunctionDefinition.Parameters[parameterIndex];

                        // Determine our stack index
                        int stackIndex = (tracepoint.Stack.Length - inputParameterCount) + parameterIndex;

                        // Create a local variable
                        LocalVariable localVariable = new LocalVariable(parameter, true, stackIndex, currentEntry);

                        // Add our local variable to our scope
                        currentScope.AddLocalVariable(localVariable);
                    }

                    // Create locals for our input parameters.
                    int outputParameterCount = currentScope.FunctionDefinition.ReturnParameters.Length;
                    for (int parameterIndex = 0; parameterIndex < outputParameterCount; parameterIndex++)
                    {
                        // Obtain our parameter
                        AstVariableDeclaration parameter = currentScope.FunctionDefinition.ReturnParameters[parameterIndex];

                        // Determine our stack index
                        int stackIndex = (tracepoint.Stack.Length + parameterIndex);

                        // Create a local variable
                        LocalVariable localVariable = new LocalVariable(parameter, true, stackIndex, currentEntry);

                        // Add our local variable to our scope
                        currentScope.AddLocalVariable(localVariable);
                    }

                    // If we have a scope depth > 0, it means we were invoked by a call, so we resolve the relevant location of the call.
                    if (currentScope.ScopeDepth > 0)
                    {
                        // Resolve the parent call by looping backwards from this scope's starting point, 
                        // finding the last call that could've led us here.
                        for (int previousTraceIndex = currentScope.StartIndex - 1; previousTraceIndex >= 0; previousTraceIndex--)
                        {
                            // Obtain the instruction number and source map entry for this previous trace index
                            (int previousInstructionNumber, SourceMapEntry previousEntry) = GetInstructionAndSourceMap(previousTraceIndex);

                            // Obtain all ast nodes for our source map entry.
                            var callNodeCandidates = AstParser.AllAstNodes.Where(a =>
                            {
                                return
                                    a.NodeType == AstNodeType.FunctionCall &&
                                    a.SourceRange.SourceIndex == previousEntry.Index &&
                                    a.SourceRange.Offset >= previousEntry.Offset &&
                                    a.SourceRange.Offset + a.SourceRange.Length <= previousEntry.Offset + previousEntry.Length;
                            }).ToArray();

                            // Verify we have obtained an item
                            // UPDATE NOTES: This used to check that the function call had "referencedDeclaration" property referencing 
                            // the FunctionDefinition, that it called.
                            if (callNodeCandidates.Length == 1)
                            {
                                // Set the calling ast node
                                currentScope.ParentFunctionCall = callNodeCandidates[0];
                                break;
                            }
                        }
                    }
                }
            }

            // Obtain all variable declarations (should be an EXACT match).
            var variableDeclarations = AstParser.GetNodes<AstVariableDeclaration>().Where(a =>
            {
                return a.SourceRange.SourceIndex == currentEntry.Index &&
                    a.SourceRange.Offset == currentEntry.Offset &&
                    a.SourceRange.Offset + a.SourceRange.Length == currentEntry.Offset + currentEntry.Length;
            }).ToArray();

            // If we have a variable declaration.
            if (variableDeclarations.Length > 0)
            {
                // If we have more than one here, throw an error
                if (variableDeclarations.Length > 1)
                {
                    throw new Exception("Multiple variable declarations were returned for one source map entry.");
                }

                // Create a local variable
                LocalVariable localVariable = new LocalVariable(variableDeclarations[0], false, tracepoint.Stack.Length, currentEntry);

                // Add our local variable to our scope
                currentScope.AddLocalVariable(localVariable);
            }
        }

        /// <summary>
        /// Parses information from <see cref="ExecutionTrace"/> in preparation for debugging operations.
        /// </summary>
        /// <param name="traceIndex">The index of the trace we want to start parsing from. Used in conjuction with depth to appropriately scan scopes.</param>
        /// <param name="scopeDepth">The depth of the current scope (also equal to the amount of parents it has).</param>
        /// <param name="callDepth">The depth of calls at this point in the scan. This refers to the EVM message depth at this point in execution.</param>
        /// <param name="parentScope">References the immediate parent scope which contains this scope. Can be null (for root).</param>
        /// <returns>Returns the next trace index to continue from for the calling scope.</returns>
        private int ParseScopes(int traceIndex, int scopeDepth, int callDepth, ExecutionTraceScope parentScope)
        {
            // Backup our scope start and create an entry in our dictionary (some properties of which we'll populate later)
            int scopeStartIndex = traceIndex;
            ExecutionTraceScope currentScope = new ExecutionTraceScope(scopeStartIndex, scopeDepth, callDepth, parentScope);

            // The last map entry obtained in the loop. 
            SourceMapEntry? lastEntry = null;

            // Loop until the end of the trace
            while (traceIndex < ExecutionTrace.Tracepoints.Length)
            {
                // Obtain our trace point for this index.
                var tracepoint = ExecutionTrace.Tracepoints[traceIndex];

                // Verify the depth matches our supposed depth
                if (callDepth > tracepoint.Depth)
                {
                    // The depth didn't match, meaning we called a contract but there were no instructions executed in it. 
                    // We close the scope instantly, returning the same trace index we started from. We don't add the scope,
                    // since it is likely to a precompile or non-existent address, and precompiles don't make deeper calls,
                    // so we have nothing to worry about here for now.
                    return traceIndex;
                }

                // Obtain our instruction number and source map
                (int instructionNumber, SourceMapEntry currentEntry) = GetInstructionAndSourceMap(traceIndex);

                // Determine if we're in an external call
                bool withinExternalCall = parentScope != null && parentScope.CallDepth < currentScope.CallDepth;

                // We determine if we're entering a scope by the opcode at this point (if it's one that calls other contracts) or by the sourcemap indicating we are jumping into a function local to this contract.
                bool currentInstructionIsCall = IsExternalCall(traceIndex);
                bool enteringScope = currentInstructionIsCall || currentEntry.Jump == JumpInstruction.Function;

                // We determine if we're exiting a scope by assuming if we're entering a scope, we can't be exiting, and if we're exiting, our depth should decrease or
                bool callDepthDecreased = (traceIndex + 1 < ExecutionTrace.Tracepoints.Length && ExecutionTrace.Tracepoints[traceIndex + 1].Depth < tracepoint.Depth);
                bool exitingScope = !enteringScope && (callDepthDecreased || (!withinExternalCall && currentEntry.Jump == JumpInstruction.Return));

                // Callstack:
                // ----------
                // We could resolve "FunctionCall" node types on scope-enter for the current source map entry, but that doesn't account for the
                // initial function we enter, since it is not called in contract code. Instead, we will rely on "FunctionDefinitions" being entered,
                // so we bundle that code in the "else" statement below which handles non-entering/exiting-scope instructions.

                // If we're entering a scope.
                if (enteringScope)
                {
                    // Determine our child scope fields
                    int childStartIndex = traceIndex + 1;
                    int childCallDepth = currentInstructionIsCall ? callDepth + 1 : callDepth;
                    int childScopeDepth = scopeDepth + 1;

                    // Parse our child scope and set our (potentially) advanced trace index
                    traceIndex = ParseScopes(childStartIndex, childScopeDepth, childCallDepth, currentScope);
                }
                else if (exitingScope)
                {
                    // If we're stepping out, we'll want to set the last step of this scope.
                    currentScope.EndIndex = traceIndex;
                    Scopes[scopeStartIndex] = currentScope;
                    return traceIndex + 1;
                }
                else
                {
                    // Parse definitions at this level. (We are not entering or exiting a scope).
                    ParseScopeTracepoint(traceIndex, currentEntry, currentScope);
                }

                // Set our last entry
                lastEntry = currentEntry;

                // Increment our trace index
                traceIndex++;
            }

            // We reached the end of our execution trace, so we should set our current scope end. (By this point traceIndex is 1 past the final)
            currentScope.EndIndex = traceIndex - 1;
            Scopes[scopeStartIndex] = currentScope;

            // Return our trace index we have ended on.
            return traceIndex;
        }

       
        public (LocalVariable variable, object value)[] GetLocalVariables()
        {
            // Obtain the local variables for the last trace point.
            return GetLocalVariables(ExecutionTrace.Tracepoints.Length - 1);
        }

        public (LocalVariable variable, object value)[] GetLocalVariables(int traceIndex)
        {
            // Obtain the scope for this trace index
            ExecutionTraceScope currentScope = GetScope(traceIndex);

            // If the scope is null, return null. Otherwise, we can also assume our trace index is valid.
            if (currentScope == null)
            {
                return null;
            }

            // Obtain our tracepoint
            ExecutionTracePoint tracePoint = ExecutionTrace.Tracepoints[traceIndex];

            // Obtain our tracepoint info.
            var currentExecutionInfo = GetInstructionAndSourceMap(traceIndex);

            // Obtain a linear representation of our memory.
            Memory<byte> memory = tracePoint.GetContiguousMemory();

            // Set our storage manager's trace index
            StorageManager.TraceIndex = traceIndex;

            // Define our result
            List<(LocalVariable variable, object value)> result = new List<(LocalVariable variable, object value)>();

            // Loop for each local variable in this scope
            foreach (LocalVariable variable in currentScope.Locals.Values)
            {
                // If our stack index hasn't been reached yet, or we haven't gotten past the function declaration in our source, we skip this variable as it is not applicable yet.
                if (variable.StackIndex >= tracePoint.Stack.Length || (!variable.IsFunctionParameter && (variable.SourceMapEntry.Offset > currentExecutionInfo.SourceMapEntry.Offset)))
                {
                    continue;
                }

                // Parse our variable's value.
                object value = variable.ValueParser.ParseFromStack(tracePoint.Stack, variable.StackIndex, memory, StorageManager);

                // Add our local variable to our results
                result.Add((variable, value));
            }

            // Return our local variable array.
            return result.ToArray();
        }

        public (StateVariable variable, object value)[] GetStateVariables()
        {
            // Obtain the state variables for the last trace point.
            return GetStateVariables(ExecutionTrace.Tracepoints.Length - 1);
        }

        public (StateVariable variable, object value)[] GetStateVariables(int traceIndex)
        {
            // Obtain the scope for this trace index
            ExecutionTraceScope currentScope = GetScope(traceIndex);

            // If the scope is null, return null. Otherwise, we can also assume our trace index is valid.
            if (currentScope == null)
            {
                return null;
            }

            // Obtain our tracepoint
            ExecutionTracePoint tracePoint = ExecutionTrace.Tracepoints[traceIndex];

            // Obtain our tracepoint info.
            var currentExecutionInfo = GetInstructionAndSourceMap(traceIndex);

            // Obtain a linear representation of our memory.
            Memory<byte> memory = tracePoint.GetContiguousMemory();

            // Set our storage manager's trace index
            StorageManager.TraceIndex = traceIndex;

            // Define our result
            List<(StateVariable variable, object value)> result = new List<(StateVariable variable, object value)>();

            // Obtain our state variables from our declarations we parsed in our current scope's contract we're in.
            // TODO: Cache all state variable declarations in the AstParser.
            AstVariableDeclaration[] stateVariableDeclarations = AstParser.GetStateVariableDeclarations(currentScope.ContractDefinition);
            var stateVariables = stateVariableDeclarations.Select(x => new StateVariable(x)).ToArray();

            // Resolve all of the storage locations for these state variables.
            StorageManager.ResolveStorageSlots(stateVariables);

            // Add the result after decoding the resulting value.
            foreach (StateVariable stateVariable in stateVariables)
            {
                // Define our variable value;
                object variableValue = null;

                // If this isn't a constant
                if (!stateVariable.Declaration.Constant)
                {
                    // Parse our variable from storage
                    variableValue = stateVariable.ValueParser.ParseFromStorage(StorageManager, stateVariable.StorageLocation);
                }

                // Add the state variable to our result list.
                result.Add((stateVariable, variableValue));
            }

            // Return our state variable array.
            return result.ToArray();
        }
        #endregion
    }
}
