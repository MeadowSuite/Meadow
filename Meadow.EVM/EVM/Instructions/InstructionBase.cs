using Meadow.EVM.Configuration;
using Meadow.EVM.EVM.Memory;
using Meadow.EVM.EVM.Messages;
using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions
{
    public abstract class InstructionBase
    {
        #region Properties
        /// <summary>
        /// Represents an opcode which describes the underlying instruction type.
        /// </summary>
        public InstructionOpcode Opcode { get; }
        /// <summary>
        /// Describes the opcode 
        /// </summary>
        public OpcodeDescriptorAttribute OpcodeDescriptor
        {
            get
            {
                // Return the descriptor for this opcode.
                return Opcode.GetDescriptor();
            }
        }

        /// <summary>
        /// The program counter location when this instruction is executed.
        /// </summary>
        public uint Offset { get; }

        /// <summary>
        /// The Ethereum Virtual Machine which this instruction exists in the context of.
        /// </summary>
        public MeadowEVM EVM { get; }
        /// <summary>
        /// The Ethereum Virtual Machine release version which we are currently operating on.
        /// </summary>
        public EthereumRelease Version
        {
            get { return EVM.Version; }
        }

        /// <summary>
        /// The Chain's ID which we are currently executing on.
        /// </summary>
        public EthereumChainID ChainID
        {
            get
            {
                return EVM.ChainID;
            }
        }

        /// <summary>
        /// The current state of gas in execution.
        /// </summary>
        public EVMGasState GasState
        {
            get { return EVM.GasState; }
        }

        /// <summary>
        /// The current runtime execution state
        /// </summary>
        public EVMExecutionState ExecutionState
        {
            get { return EVM.ExecutionState; }
        }

        /// <summary>
        /// The current Ethereum Virtual Machine's stack
        /// </summary>
        public EVMStack Stack
        {
            get { return EVM.ExecutionState.Stack; }
        }

        /// <summary>
        /// The current Ethereum Virtual Machine's memory
        /// </summary>
        public EVMMemory Memory
        {
            get { return EVM.ExecutionState.Memory; }
        }

        /// <summary>
        /// The current Ethereum Virtual Machine's message it is executing on.
        /// </summary>
        public EVMMessage Message
        {
            get { return EVM.Message; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionBase(MeadowEVM evm)
        {
            // Set our virtual machine
            EVM = evm;

            // Set our code address in case we need to know it (by execution time it will be advanced past it)
            Offset = ExecutionState.PC;

            // We read our opcode
            Opcode = (InstructionOpcode)evm.Code.Span[(int)Offset];

            // Advance our program counter
            ExecutionState.PC++;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Executes the given instruction in the given execution state.
        /// </summary>
        public abstract void Execute();
        /// <summary>
        /// Sets the results of our execution to mark execution as concluded.
        /// </summary>
        /// <param name="executionResult">The result of the execution.</param>
        public void Return(EVMExecutionResult executionResult)
        {
            // Set the result of our execution.
            ExecutionState.Result = executionResult;
        }

        /// <summary>
        /// Obtains a string representation of our instruction
        /// </summary>
        /// <returns>Returns a string representation of our instruction.</returns>
        public override string ToString()
        {
            return Opcode.ToString();
        }
        #endregion
    }
}
