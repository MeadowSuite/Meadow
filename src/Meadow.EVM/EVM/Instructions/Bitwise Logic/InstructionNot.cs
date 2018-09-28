using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Bitwise_Logic
{
    public class InstructionNot : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionNot(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Perform a bitwise NOT on the first two items we pop off the stack and push the result back onto the stack.
            BigInteger a = Stack.Pop();

            // Our value is bound to this maximum, any NOT operation should bring the minimum (0) to the maximum (UINT256_MAX_VALUE) and vice versa. All values should be proportionate between.
            BigInteger result = EVMDefinitions.UINT256_MAX_VALUE - a;

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
