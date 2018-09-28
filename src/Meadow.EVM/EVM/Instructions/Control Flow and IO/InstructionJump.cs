using Meadow.EVM.EVM.Execution;
using Meadow.EVM.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.Control_Flow_and_IO
{
    public class InstructionJump : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionJump(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Set our program counter to the value we pop off of the stack.
            BigInteger destination = Stack.Pop();
            if (destination >= EVM.Code.Length)
            {
                throw new EVMException($"{Opcode} jumped outside the bounds of code: Jumped to offset {destination}.");
            }

            ExecutionState.PC = (uint)destination;

            // Indicate we have jumped to our execution state.
            ExecutionState.JumpedLastInstruction = true;
        }
        #endregion
    }
}