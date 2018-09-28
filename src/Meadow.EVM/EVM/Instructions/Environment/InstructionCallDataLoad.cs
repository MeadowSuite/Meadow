using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Environment
{
    public class InstructionCallDataLoad : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionCallDataLoad(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain our offset into call data we want to start copying at.
            BigInteger offset = Stack.Pop();

            // Read our data (if our offset is wrong or we hit the end of the array, the rest should be zeroes).
            byte[] data = new byte[EVMDefinitions.WORD_SIZE];
            int length = data.Length;
            if (offset > Message.Data.Length)
            {
                offset = 0;
                length = 0;
            }
            else if (offset + length > Message.Data.Length)
            {
                length = Message.Data.Length - (int)offset;
            }
       
            // Copy however much data we were able to out of our 32-byte desired count.
            Array.Copy(Message.Data, (int)offset, data, 0, length);

            // Convert it to a big integer and push it.
            Stack.Push(BigIntegerConverter.GetBigInteger(data));
        }
        #endregion
    }
}
