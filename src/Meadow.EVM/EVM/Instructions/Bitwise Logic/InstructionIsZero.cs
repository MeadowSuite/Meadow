using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Bitwise_Logic
{
    public class InstructionIsZero : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionIsZero(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Checks that the assumed comparison is valid. If it is, return 1, otherwise return 0.
            BigInteger a = Stack.Pop();
            BigInteger result = a == 0 ? 1 : 0;

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
