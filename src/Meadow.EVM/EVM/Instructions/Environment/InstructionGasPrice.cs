using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Environment
{
    public class InstructionGasPrice : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionGasPrice(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Push the transaction's current gas price onto the stack.
            if (EVM.State.CurrentTransaction != null)
            {
                Stack.Push(EVM.State.CurrentTransaction.GasPrice);
            }
            else
            {
                Stack.Push(0);
            }
        }
        #endregion
    }
}
