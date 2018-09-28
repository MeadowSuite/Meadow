using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Environment
{
    public class InstructionOrigin : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionOrigin(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Push the origin address from the transaction to the stack.
            if (EVM.State.CurrentTransaction != null)
            {
                Stack.Push(EVM.State.CurrentTransaction.GetSenderAddress());
            }
            else
            {
                Stack.Push(0);
            }
        }
        #endregion
    }
}
