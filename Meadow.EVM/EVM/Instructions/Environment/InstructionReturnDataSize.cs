using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Data_Types;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Environment
{
    public class InstructionReturnDataSize : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionReturnDataSize(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain our return data length
            int codeLength = ExecutionState.LastCallResult?.ReturnData == null ? 0 : ExecutionState.LastCallResult.ReturnData.Length;

            // Push the return data length to the stack.
            Stack.Push(codeLength);
        }
        #endregion
    }
}
