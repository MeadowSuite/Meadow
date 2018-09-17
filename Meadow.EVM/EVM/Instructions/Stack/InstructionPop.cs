using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.Stack
{
    public class InstructionPop : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionPop(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // We'll want to pop an item off of the stack.
            Stack.Pop();
        }
        #endregion
    }
}
