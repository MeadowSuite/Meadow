using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Stack
{
    public class InstructionSwap : InstructionBase
    {
        #region Properties
        public uint SwapIndex { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionSwap(MeadowEVM evm) : base(evm)
        {
            // This class handles multiple swap operations (various indexes).
            // The opcodes are linear, so we can calculate the index based off opcode.
            SwapIndex = (uint)(Opcode - InstructionOpcode.SWAP1) + 1;
        }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Swap the top item on the stack with the one at the provided index.
            Stack.Swap(SwapIndex);
        }
        #endregion
    }
}
