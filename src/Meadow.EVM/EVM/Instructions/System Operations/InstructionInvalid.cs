using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.Exceptions;
using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.System_Operations
{
    public class InstructionInvalid : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionInvalid(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Throw our invalid instruction exception
            throw new EVMException($"{Opcode.ToString()} instruction hit!");
        }
        #endregion
    }
}
