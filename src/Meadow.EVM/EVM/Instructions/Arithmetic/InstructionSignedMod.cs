using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Arithmetic
{
    public class InstructionSignedMod : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionSignedMod(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Modulo divide the two signed words off of the top of the stack.
            BigInteger a = Stack.Pop(true);
            BigInteger b = Stack.Pop(true);
            BigInteger result = 0;
            if (a != 0)
            {
                result = a.Sign * (BigInteger.Abs(a) % BigInteger.Abs(b));
            }

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
