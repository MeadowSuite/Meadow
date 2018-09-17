using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Bitwise_Logic
{
    public class InstructionExtractByte : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionExtractByte(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain the index and value to extract the byte at the given index from the value
            BigInteger index = Stack.Pop(); // determines the byte to extract (0 is left side, big endian)
            BigInteger value = Stack.Pop();

            BigInteger result = 0;
            if (index < EVMDefinitions.WORD_SIZE)
            {
                // Extract the given byte
                int extractionIndex = (EVMDefinitions.WORD_SIZE - 1) - (int)index;
                result = value >> ((int)extractionIndex * 8);
                result &= 0xFF;
            }

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
