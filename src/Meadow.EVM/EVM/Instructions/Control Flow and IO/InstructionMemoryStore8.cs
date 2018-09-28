using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.Control_Flow_and_IO
{
    public class InstructionMemoryStore8 : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionMemoryStore8(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain the address at which to read memory from in our memory segment.
            BigInteger address = Stack.Pop();

            // Obtain our data to store
            BigInteger data = Stack.Pop();
            byte storeData = (byte)(data & 0xFF);

            // Store our data in our memory segment at the given address
            Memory.Write((long)address, storeData);
        }
        #endregion
    }

}
