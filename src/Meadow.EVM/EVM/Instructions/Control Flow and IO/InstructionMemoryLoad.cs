using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.Control_Flow_and_IO
{
    public class InstructionMemoryLoad : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionMemoryLoad(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain the address at which to read memory from in our memory segment.
            BigInteger address = Stack.Pop();

            // Read a 256-bit integer from our memory segment and append it to stack
            BigInteger value = Memory.ReadBigInteger((long)address);

            // Push our value back onto the stack.
            Stack.Push(value);
        }
        #endregion
    }

}
