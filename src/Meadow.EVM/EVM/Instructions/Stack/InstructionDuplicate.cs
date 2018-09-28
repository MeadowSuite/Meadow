using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Stack
{
    public class InstructionDuplicate : InstructionBase
    {
        #region Properties
        public uint DuplicateIndex { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionDuplicate(MeadowEVM evm) : base(evm)
        {
            // This class handles multiple duplicate operations (various indexes).
            // The opcodes are linear, so we can calculate the index based off opcode.
            DuplicateIndex = (Opcode - InstructionOpcode.DUP1);
        }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Creates a duplicate of the item at the given index on the stack, and puts it at the top of the stack.
            Stack.Duplicate(DuplicateIndex);
        }
        #endregion
    }
}
