using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Environment
{
    public class InstructionCallDataSize : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionCallDataSize(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Push the call data size onto the stack.
            Stack.Push(Message.Data.Length);
        }
        #endregion
    }
}
