using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.Core.Utils;
using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Execution;

namespace Meadow.EVM.EVM.Instructions.Arithmetic
{
    public class InstructionExponent : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionExponent(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain our value and exponent
            BigInteger value = Stack.Pop();
            BigInteger exponent = Stack.Pop();

            // Calculate how much gas to deduct: the cost per exponent byte differs per Ethereum release
            byte[] exponentBytes = BigIntegerConverter.GetBytesWithoutLeadingZeros(exponent);
            BigInteger extraGasCost = exponentBytes.Length;
            if (Version < EthereumRelease.SpuriousDragon)
            {
                // Pre-Spurious Dragon
                extraGasCost *= GasDefinitions.GAS_EXP_BYTE;
            }
            else
            {
                // Spurious Dragon+
                extraGasCost *= GasDefinitions.GAS_EXP_BYTE_SPURIOUS_DRAGON;
            }

            // Deduct our gas
            GasState.Deduct(extraGasCost);
           
            // Perform an exponent operation on the two unsigned words off of the top of the stack.
            BigInteger result = BigInteger.ModPow(value, exponent, EVMDefinitions.UINT256_MAX_VALUE + 1);

            // Push the result onto the stack.
            Stack.Push(result);
        }
        #endregion
    }
}
