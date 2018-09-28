using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Cryptography
{
    public class InstructionSHA3 : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionSHA3(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain the memory offset and size
            BigInteger offset = Stack.Pop();
            BigInteger size = Stack.Pop();

            // Deduct our gas accordingly.
            GasState.Deduct(EVMDefinitions.GetWordCount(size) * GasDefinitions.GAS_SHA3_WORD);

            // Read our data to hash
            byte[] data = Memory.ReadBytes((long)offset, (int)size);

            // Hash our read data.
            byte[] hash = KeccakHash.ComputeHashBytes(data);

            // Push it onto our stack.
            Stack.Push(BigIntegerConverter.GetBigInteger(hash));
        }
        #endregion
    }
}
