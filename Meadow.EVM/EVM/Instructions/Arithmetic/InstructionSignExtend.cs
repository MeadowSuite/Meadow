using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Arithmetic
{
    public class InstructionSignExtend : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionSignExtend(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // TODO: This all needs verification, especially bitwise operators on BigInteger!!

            // Obtain the index and value to extend the sign of. In this instruction, we look for the sign at the given byte index and extend it across all leading bytes.
            BigInteger index = Stack.Pop(); // determines the byte to check for the sign bit in.
            BigInteger value = Stack.Pop();

            BigInteger result = value;
            if (index < EVMDefinitions.WORD_SIZE)
            {
                // Obtain a mask for our signbit.
                BigInteger signMask = 1;
                signMask <<= (((int)index * 8) + 7);

                if ((value & signMask) != 0)
                {
                    // Sign bit is set, so we also want to set all bits leading up to it. Taking the 0x10000... ceiling of max value, if we subtract the sign bit, it'll give us a number with only all those bits set, which we can use to set all bits.
                    result = value | ((EVMDefinitions.UINT256_MAX_VALUE + 1) - signMask);
                }
                else
                {
                    // Sign bit is not set, we'll want all bits leading up to it to be zero'd as well. We'll want to mask everything after the sign bit.
                    result = value & (signMask - 1);
                }
            }

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
