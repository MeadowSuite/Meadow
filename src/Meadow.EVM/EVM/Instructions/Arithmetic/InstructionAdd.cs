using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Arithmetic
{
    public class InstructionAdd : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionAdd(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Add two unsigned words off of the top of the stack.
            BigInteger result = Stack.Pop() + Stack.Pop();

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
