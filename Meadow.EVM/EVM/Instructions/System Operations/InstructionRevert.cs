using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.System_Operations
{
    public class InstructionRevert : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionRevert(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain the offset and size of the data to copy from memory as our result.
            BigInteger offset = Stack.Pop();
            BigInteger size = Stack.Pop();

            // We'll want to return with our read memory, our remaining gas, and indicating we wish to revert changes.
            Return(new EVMExecutionResult(EVM, Memory.ReadBytes((long)offset, (int)size), GasState.Gas, false));
        }
        #endregion
    }
}
