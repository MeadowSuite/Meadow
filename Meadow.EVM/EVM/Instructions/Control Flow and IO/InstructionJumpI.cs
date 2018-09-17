using Meadow.EVM.EVM.Execution;
using Meadow.EVM.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.Control_Flow_and_IO
{
    public class InstructionJumpI : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionJumpI(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Set our program counter to the value we pop off of the stack.
            BigInteger destination = Stack.Pop();
            BigInteger condition = Stack.Pop();
            bool jumping = condition != 0;

            // If the condition is set, jump to our destination.
            if (jumping)
            {
                // Set our program counter
                if (destination >= EVM.Code.Length)
                {
                    throw new EVMException($"{Opcode} jumped outside the bounds of code.");
                }

                ExecutionState.PC = (uint)destination;

                // Indicate we have jumped to our execution state.
                ExecutionState.JumpedLastInstruction = true;
            }

            // Record our coverage for this execution.
            EVM.CoverageMap?.RecordBranch(Offset, jumping);
        }
        #endregion
    }
}