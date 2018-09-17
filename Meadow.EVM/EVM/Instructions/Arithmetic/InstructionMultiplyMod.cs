using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.EVM.Execution;
using Meadow.Core.Utils;

namespace Meadow.EVM.EVM.Instructions.Arithmetic
{
    public class InstructionMultiplyMod : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionMultiplyMod(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Modulo divide the two multiplied unsigned words off of the top of the stack.
            BigInteger a = Stack.Pop();
            BigInteger b = Stack.Pop();
            BigInteger c = Stack.Pop();
            BigInteger result = c == 0 ? 0 : (a * b).CapOverflow() % c;

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
