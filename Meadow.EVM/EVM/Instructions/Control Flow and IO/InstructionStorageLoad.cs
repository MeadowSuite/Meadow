using Meadow.EVM.Data_Types.Accounts;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.Control_Flow_and_IO
{
    public class InstructionStorageLoad : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionStorageLoad(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Pop our key off of the stack.
            BigInteger key = Stack.Pop();

            // In our current account, check the storage for this key's value and push it to the stack.
            Stack.Push(EVM.State.GetStorageData(Message.To, key));
        }
        #endregion
    }
}
