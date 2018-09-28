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
using Meadow.EVM.Exceptions;

namespace Meadow.EVM.EVM.Instructions.System_Operations
{
    public class InstructionCall : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionCall(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain all of our values for the call (the value variable is only used in some calls, and is zero otherwise)
            BigInteger gas = Stack.Pop(); // The base amount of gas we allocate to the call.
            Address to = Stack.Pop(); // The address we're making the call to.

            BigInteger value = 0;
            if (Opcode == InstructionOpcode.CALL || Opcode == InstructionOpcode.CALLCODE)
            {
                value = Stack.Pop();
            }

            // Obtain the values for where our input memory comes from, and where our output memory will go to.
            BigInteger inputMemoryStart = Stack.Pop();
            BigInteger inputMemorySize = Stack.Pop();
            BigInteger outputMemoryStart = Stack.Pop();
            BigInteger outputMemorySize = Stack.Pop();

            // CALL opcode can only make static calls with a zero value.
            if (Opcode == InstructionOpcode.CALL && Message.IsStatic && value != 0)
            {
                throw new EVMException($"Cannot use opcode {Opcode.ToString()} in a static context call with any value but zero.");
            }

            // Gas: Pre-Expand Memory (since it should be charged for expansion, then the call should be made, then written to)
            Memory.ExpandStream(inputMemoryStart, inputMemorySize);
            Memory.ExpandStream(outputMemoryStart, outputMemorySize);

            // Gas: Calculate extra gas costs based off of forks and what kind of call this is.
            BigInteger extraGasCost = 0;

            // Gas: If this is a call and the account doesn't exist
            if (Opcode == InstructionOpcode.CALL && !EVM.State.ContainsAccount(to))
            {
                // If the value is above zero (or if we're pre-spurious dragon) we charge for calling a new account.
                if (value > 0 || Version < EthereumRelease.SpuriousDragon)
                {
                    extraGasCost = GasDefinitions.GAS_CALL_NEW_ACCOUNT;
                }
            }

            // If we are transferring a value, we charge gas
            if (value > 0)
            {
                extraGasCost += GasDefinitions.GAS_CALL_VALUE;
            }

            // Tangerine whistle introduces new inner call gas limits
            // Source: https://github.com/ethereum/EIPs/blob/master/EIPS/eip-150.md
            if (Version < EthereumRelease.TangerineWhistle)
            {
                // Prior to tangerine whistle, we need the provided gas + extra gas available
                GasState.Check(gas + extraGasCost);
            }
            else
            {
                // After tangerine whistle, we check that the desired gas amount for the call doesn't exceed our calculated max call gas (or else it's capped).
                GasState.Check(extraGasCost);
                gas = BigInteger.Min(gas, GasDefinitions.GetMaxCallGas(GasState.Gas - extraGasCost));
            }

            // Define how much gas our inner message can take.
            BigInteger innerCallGas = gas;
            if (value > 0)
            {
                innerCallGas += GasDefinitions.GAS_CALL_VALUE_STIPEND;
            }

            // Verify we have enough balance and call depth hasn't exceeded the maximum.
            if (EVM.State.GetBalance(Message.To) >= value && Message.Depth < EVMDefinitions.MAX_CALL_DEPTH)
            {
                // We're going to make an inner call, so we charge our gas and extra gas.
                GasState.Deduct(gas + extraGasCost);

                // Obtain our call data.
                byte[] callData = Memory.ReadBytes((long)inputMemoryStart, (int)inputMemorySize);

                // Create our message
                EVMMessage innerMessage = null;
                switch (Opcode)
                {
                    case InstructionOpcode.CALL:
                        innerMessage = new EVMMessage(Message.To, to, value, innerCallGas, callData, Message.Depth + 1, to, true, Message.IsStatic);
                        break;
                    case InstructionOpcode.DELEGATECALL:
                        innerMessage = new EVMMessage(Message.Sender, Message.To, Message.Value, innerCallGas, callData, Message.Depth + 1, to, false, Message.IsStatic);
                        break;
                    case InstructionOpcode.STATICCALL:
                        innerMessage = new EVMMessage(Message.To, to, value, innerCallGas, callData, Message.Depth + 1, to, true, true);
                        break;
                    case InstructionOpcode.CALLCODE:
                        innerMessage = new EVMMessage(Message.To, Message.To, value, innerCallGas, callData, Message.Depth + 1, to, true, Message.IsStatic);
                        break;
                }

                // Execute our message in an inner VM.
                EVMExecutionResult innerVMResult = MeadowEVM.Execute(EVM.State, innerMessage);

                // Refund our remaining gas that the inner VM didn't use.
                GasState.Refund(innerVMResult.RemainingGas);

                // Set our last call results
                ExecutionState.LastCallResult = innerVMResult;

                // Push a status indicating whether execution had succeeded without reverting changes.
                if (!innerVMResult.Succeeded)
                {
                    Stack.Push(0);
                }
                else
                {
                    Stack.Push(1);
                }

                // Determine how much we want to copy out.
                int returnCopyLength = Math.Min(ExecutionState.LastCallResult?.ReturnData.Length ?? 0, (int)outputMemorySize);
                if (returnCopyLength == 0 || ExecutionState.LastCallResult?.ReturnData == null)
                {
                    return;
                }

                // Copy our data out
                Memory.Write((long)outputMemoryStart, ExecutionState.LastCallResult.ReturnData.ToArray());
            }
            else
            {
                // We didn't have a sufficient balance or call depth so we push nothing to the stack. We push 0 (fail)
                Stack.Push(0);

                // Set our last call result as null.
                ExecutionState.LastCallResult = null;

                // Since we couldn't make an inner message call, we charge all the other extra charges, but not the inner message call cost. (Note: inner call gas comes from gas, so we put it here to offset potential extra cost from stipend)
                GasState.Deduct(gas + extraGasCost - innerCallGas);
            }
        }
        #endregion
    }
}
