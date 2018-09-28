using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Arithmetic
{
    public class InstructionSubtract : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionSubtract(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Subtract two unsigned words off of the top of the stack from eachother.
            BigInteger a = Stack.Pop();
            BigInteger b = Stack.Pop();
            BigInteger result = a - b;

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
