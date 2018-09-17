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
    public class InstructionExternalCodeSize : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionExternalCodeSize(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain our external account address
            Address externalCodeAddress = Stack.Pop();

            // Obtain our code segment
            byte[] codeSegment = EVM.State.GetCodeSegment(externalCodeAddress);

            // Push the code size to the stack.
            Stack.Push(codeSegment.Length);
        }
        #endregion
    }
}
