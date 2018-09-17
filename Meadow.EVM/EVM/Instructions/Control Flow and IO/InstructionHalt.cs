using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.Control_Flow_and_IO
{
    public class InstructionHalt : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionHalt(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // We'll want to return with a blank result, our remaining gas, and a status indicating whether we should revert.
            Return(new EVMExecutionResult(EVM, null, true));
        }
        #endregion
    }
}
