using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Bitwise_Logic
{
    public class InstructionXor : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionXor(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Perform a bitwise XOR on the first two items we pop off the stack and push the result back onto the stack.
            BigInteger a = Stack.Pop();
            BigInteger b = Stack.Pop();
            BigInteger result = a ^ b;

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
