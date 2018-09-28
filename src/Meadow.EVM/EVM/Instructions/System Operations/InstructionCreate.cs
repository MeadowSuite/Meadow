using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Messages;
using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types;
using Meadow.EVM.Exceptions;
using Meadow.Core.Utils;

namespace Meadow.EVM.EVM.Instructions.System_Operations
{
    public class InstructionCreate : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionCreate(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain the values for our call value, and call data memory.
            BigInteger value = Stack.Pop();
            BigInteger inputMemoryStart = Stack.Pop();
            BigInteger inputMemorySize = Stack.Pop();

            // We'll want to charge for memory expansion first
            Memory.ExpandStream(inputMemoryStart, inputMemorySize);

            // If we're in a static context, we can't self destruct
            if (Message.IsStatic)
            {
                throw new EVMException($"{Opcode.ToString()} instruction cannot execute in a static context!");
            }

            // Verify we have enough balance and call depth hasn't exceeded the maximum.
            if (EVM.State.GetBalance(Message.To) >= value && Message.Depth < EVMDefinitions.MAX_CALL_DEPTH)
            {
                // Obtain our call information.
                byte[] callData = Memory.ReadBytes((long)inputMemoryStart, (int)inputMemorySize);
                BigInteger innerCallGas = GasState.Gas;
                if (Version >= EthereumRelease.TangerineWhistle)
                {
                    innerCallGas = GasDefinitions.GetMaxCallGas(innerCallGas);
                }

                // Create our message
                EVMMessage message = new EVMMessage(Message.To, Address.ZERO_ADDRESS, value, innerCallGas, callData, Message.Depth + 1, Address.ZERO_ADDRESS, true, Message.IsStatic);
                EVMExecutionResult innerVMResult = MeadowEVM.CreateContract(EVM.State, message);
                if (innerVMResult.Succeeded)
                {
                    // Push our resulting address onto the stack.
                    Stack.Push(BigIntegerConverter.GetBigInteger(innerVMResult.ReturnData.ToArray()));
                    EVM.ExecutionState.LastCallResult = null;
                }
                else
                {
                    // We failed, push our fail value and put the last call data in place.
                    Stack.Push(0);
                    ExecutionState.LastCallResult = innerVMResult;
                }
            }
            else
            {
                // We didn't have a sufficient balance or call depth so we push nothing to the stack. We push 0 (fail)
                Stack.Push(0);

                // Set our last call result as null.
                ExecutionState.LastCallResult = null;
            }
        }
        #endregion
    }
}
