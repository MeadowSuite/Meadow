using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Instructions;
using Meadow.EVM.EVM.Instructions.Control_Flow_and_IO;
using Meadow.EVM.EVM.Messages;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Debugging.Tracing
{
    public class ExecutionTrace
    {
        #region Fields
        /// <summary>
        /// The last contract address we had context in during this trace.
        /// </summary>
        private Address _lastContractAddress;
        /// <summary>
        /// The last memory change count we had recorded.
        /// </summary>
        private ulong _lastMemoryChangeCount;
        #endregion

        #region Properties
        /// <summary>
        /// Lists all points traced during this execution, providing information at every step of execution.
        /// </summary>
        public List<ExecutionTracePoint> Tracepoints { get; private set; }
        /// <summary>
        /// Lists all exceptions that occurred during this execution, providing the index in <see cref="Tracepoints"/> at which it occurred, and the exception itself.
        /// </summary>
        public List<ExecutionTraceException> Exceptions { get; private set; }
        #endregion

        #region Constructor
        public ExecutionTrace()
        {
            // Set our default values
            Tracepoints = new List<ExecutionTracePoint>();
            Exceptions = new List<ExecutionTraceException>();
            _lastContractAddress = null;
            _lastMemoryChangeCount = 0;
        }
        #endregion

        #region Functions
        public void RecordExecution(MeadowEVM evm, InstructionBase instruction, BigInteger gasRemaining, BigInteger gasCost)
        {
            // Obtain our deployed address and status
            Address deployedAddress = evm.Message.GetDeployedCodeAddress();
            bool isDeployed = deployedAddress == evm.Message.CodeAddress;

            // Declare our memory + last returned data, null initially (indicating it is unchanged from the last known value).
            byte[] memory = null;

            // Determine if our context has changed between contracts, or our memory has changed.
            bool contextChanged = _lastContractAddress != deployedAddress;
            bool memoryChanged = _lastMemoryChangeCount != evm.ExecutionState.Memory.ChangeCount;

            // If our context or memory has changed, we'll want to include the memory. (This way our simple lookup for memory will consist of checking in the current trace point, or if null, loop backwards to the last one you find).
            if (contextChanged || memoryChanged)
            {
                memory = evm.ExecutionState.Memory.ToArray();
            }

            // Update our last processed contract address and change count
            _lastContractAddress = deployedAddress;
            _lastMemoryChangeCount = evm.ExecutionState.Memory.ChangeCount;

            // Determine if our storage changed
            bool storageChanged = instruction is InstructionStorageStore;

            // Determine our storage we will have for this point (null if unchanged, otherwise set)
            Dictionary<Memory<byte>, byte[]> storage = null;
            if (contextChanged || storageChanged)
            {
                storage = evm.State.GetAccount(evm.Message.To).StorageTrie.ToDictionary();
            }

            // If our context changed, we want to include code hash.
            byte[] codeSegment = null;
            if (contextChanged)
            {
                codeSegment = evm.Code.ToArray();
            }

            // Add a new tracepoint.
            var tracePoint = new ExecutionTracePoint()
            {
                Code = codeSegment,
                ContractAddress = contextChanged ? deployedAddress : null,
                ContractDeployed = isDeployed,
                Depth = (uint)evm.Message.Depth,
                Opcode = instruction.OpcodeDescriptor.Mnemonic,
                PC = instruction.Offset,
                GasRemaining = gasRemaining,
                GasCost = gasCost,
                Stack = evm.ExecutionState.Stack.ToArray(),
                Memory = memory,
                Storage = storage
            };

            // Add it to our list.
            Tracepoints.Add(tracePoint);
        }

        public void RecordException(Exception exception, bool isContractExecuting)
        {
            // If we have an EVM instance, then we want to tie the exception to a trace index, otherwise it was out of the EVM execution (block error or etc).
            int? traceIndex = isContractExecuting ? Math.Max(Tracepoints.Count - 1, 0) : (int?)null;

            // Add our exception to our list
            Exceptions.Add(new ExecutionTraceException(traceIndex, exception));
        }
        #endregion
    }
}
