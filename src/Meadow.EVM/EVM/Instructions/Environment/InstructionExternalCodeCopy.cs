using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Environment
{
    public class InstructionExternalCodeCopy : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionExternalCodeCopy(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain our code address
            Address externalCodeAddress = Stack.Pop();

            // Obtain our code segment
            byte[] codeSegment = EVM.State.GetCodeSegment(externalCodeAddress);

            // Obtain our offsets and sizes for the copy.
            BigInteger memoryOffset = Stack.Pop();
            BigInteger dataOffset = Stack.Pop();
            BigInteger dataSize = Stack.Pop();

            // This is considered a copy operation, so we charge for the size of the data.
            GasState.Deduct(GasDefinitions.GetMemoryCopyCost(Version, dataSize));

            // Read our data from the code segment (if our offset is wrong or we hit the end of the array, the rest should be zeroes).
            byte[] data = new byte[(int)dataSize];
            int length = data.Length;
            if (dataOffset > codeSegment.Length)
            {
                dataOffset = 0;
                length = 0;
            }
            else if (dataOffset + length > codeSegment.Length)
            {
                length = codeSegment.Length - (int)dataOffset;
            }

            // Copy to the memory location.
            Array.Copy(codeSegment, (int)dataOffset, data, 0, length);

            // Write the data to our given memory offset.
            Memory.Write((long)memoryOffset, data);
        }
        #endregion
    }
}
