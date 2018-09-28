using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Environment
{
    public class InstructionCaller : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionCaller(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Push the sender address of the message to the stack.
            Stack.Push(Message.Sender);
        }
        #endregion
    }
}
