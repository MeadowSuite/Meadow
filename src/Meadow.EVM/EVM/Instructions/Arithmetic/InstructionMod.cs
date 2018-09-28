using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Arithmetic
{
    public class InstructionMod : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionMod(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Modulo divide the two unsigned words off of the top of the stack.
            BigInteger a = Stack.Pop();
            BigInteger b = Stack.Pop();
            BigInteger result = b == 0 ? 0 : a % b;

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
