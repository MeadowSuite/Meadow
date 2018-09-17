using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.Block_Information
{
    public class InstructionBlockGasLimit : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionBlockGasLimit(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // We push our gas limit onto the stack.
            Stack.Push(EVM.State.CurrentBlock.Header.GasLimit);
        }
        #endregion
    }

}
