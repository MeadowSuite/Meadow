using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Data_Types;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.Exceptions;
using Meadow.EVM.EVM.Execution;
using Meadow.EVM.Configuration;

namespace Meadow.EVM.EVM.Instructions.Environment
{
    public class InstructionReturnDataCopy : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionReturnDataCopy(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain our offsets and sizes for the copy.
            BigInteger memoryOffset = Stack.Pop();
            BigInteger dataOffset = Stack.Pop();
            BigInteger dataSize = Stack.Pop();

            // This is considered a copy operation, so we charge for the size of the data.
            GasState.Deduct(GasDefinitions.GetMemoryCopyCost(Version, dataSize));

            // If we aren't copying anything, we can stop.
            if (dataOffset + dataSize == 0)
            {
                return;
            }

            // Check we have a return result, and check it's bounds.
            if (ExecutionState.LastCallResult?.ReturnData == null)
            {
                throw new EVMException($"{Opcode.ToString()} tried to copy return data from last call, but no last return data exists.");
            }
            else if (dataOffset + dataSize > (ExecutionState.LastCallResult?.ReturnData.Length ?? 0))
            {
                throw new EVMException($"{Opcode.ToString()} tried to copy return data past the end.");
            }
            else
            {
                // Otherwise we write our data we wish to copy to memory.
                Memory.Write((long)memoryOffset, ExecutionState.LastCallResult.ReturnData.Slice((int)dataOffset, (int)(dataSize)).ToArray());
            }
        }
        #endregion
    }
}
