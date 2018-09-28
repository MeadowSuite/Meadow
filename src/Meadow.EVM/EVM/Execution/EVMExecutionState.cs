using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Memory;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Execution
{
    public class EVMExecutionState
    {
        #region Properties
        /// <summary>
        /// Represents the expandable memory segment for the Ethereum Virtual Machine.
        /// </summary>
        public EVMMemory Memory { get; set; }
        /// <summary>
        /// Represents the stack for the Ethereum Virtual Machine.
        /// </summary>
        public EVMStack Stack { get; set; }
        /// <summary>
        /// Represents the program counter for the Ethereum Virtual Machine, indicating the address where the next instruction will be fetched and executed.
        /// </summary>
        public uint PC { get; set; }
        /// <summary>
        /// The amount of gas remaining for this execution.
        /// </summary>
        public BigInteger Gas { get; set; }

        /// <summary>
        /// Indicates whether the last instruction we processed was a jump. This is used to look for a JUMPDEST instruction where we expect to jump to. If a jump occurs to a place with anything else, an exception will be thrown.
        /// </summary>
        public bool JumpedLastInstruction { get; set; }
        /// <summary>
        /// Represents the returning result of this VM's execution.
        /// </summary>
        public EVMExecutionResult Result { get; set; }
        /// <summary>
        /// Represents the last returning result of another VM as a result of a call this one made.
        /// </summary>
        public EVMExecutionResult LastCallResult { get; set; }
        #endregion

        #region Constructors
        public EVMExecutionState(MeadowEVM evm)
        {
            // Initialize a new memory segment.
            Memory = new EVMMemory(evm);

            // Initialize the stack.
            Stack = new EVMStack();

            // Set the program counter.
            PC = 0;
        }
        #endregion
    }
}
