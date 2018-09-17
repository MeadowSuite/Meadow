using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Execution;
using Meadow.EVM.Exceptions;

namespace Meadow.EVM.EVM.Instructions.Stack
{
    public class InstructionPush : InstructionBase
    {
        #region Properties
        public uint PushSize { get; }
        public BigInteger PushData { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionPush(MeadowEVM evm) : base(evm)
        {
            // This class handles multiple push operations (various sizes).
            // The opcodes are linear, so we can calculate the size of the push based off opcode.
            PushSize = (uint)(Opcode - InstructionOpcode.PUSH1) + 1;

            // Assert we are not at the end of the code.
            if (EVM.Code.Length < (ExecutionState.PC + PushSize))
            {
                throw new EVMException($"Cannot read {OpcodeDescriptor.Mnemonic}'s operand because the end of the stream was reached, or the bytes to read were unavailable.");
            }

            // Read our push data.
            byte[] pushBytes = EVM.Code.Slice((int)ExecutionState.PC, (int)PushSize).ToArray();

            // Parse our push data as a uint256.
            PushData = BigIntegerConverter.GetBigInteger(pushBytes);

            // Advance our program counter
            ExecutionState.PC += PushSize;
        }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Push the result onto the stack.
            Stack.Push(PushData, PushSize);
        }

        /// <summary>
        /// Obtains a string representation of our instruction
        /// </summary>
        /// <returns>Returns a string representation of our instruction.</returns>
        public override string ToString()
        {
            return $"{Opcode} 0x{BigIntegerConverter.GetBytes(PushData, (int)PushSize).ToHexString()}";
        }
        #endregion
    }
}
