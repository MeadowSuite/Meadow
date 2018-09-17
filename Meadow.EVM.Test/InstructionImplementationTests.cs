using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types.State;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Instructions;
using System;
using System.IO;
using Xunit;

namespace Meadow.EVM.Test
{
    public class InstructionImplementationTests
    {
        /*
         * We'll want to test implementations here. 
         * 
         * Standard tests should include symbolic execution which check counts of items removed from or added to the stack, using the opcode descriptor for expected values.
         * */

         /// <summary>
         /// This function creates an evm instance and computes every instruction on a pre-populated stack and fresh genesis state. It skips instructions that throw errors in case we couldn't fuzz them, but acts as a preliminary check for stack/operational issues.
         /// </summary>
         [Fact]
         public void InstructionStackChangeTest()
        {
            // Get a list of all opcodes
            InstructionOpcode[] opcodes = (InstructionOpcode[])Enum.GetValues(typeof(InstructionOpcode));

            // Create a new state
            var configuration = new Configuration.Configuration();
            State state = configuration.GenesisStateSnapshot.ToState();

            // Create an EVM.
            var evm = new MeadowEVM();
            evm.State = state;
            evm.ExecutionState = new Meadow.EVM.EVM.Execution.EVMExecutionState(evm);
            evm.GasState = new Meadow.EVM.EVM.Execution.EVMGasState(int.MaxValue);
            evm.Message = new Meadow.EVM.EVM.Messages.EVMMessage("0", "0", 0, int.MaxValue, Array.Empty<byte>(), 0, "0", true, false);
            evm.Code = new byte[EVMDefinitions.MAX_CONTRACT_SIZE];

            // Push a bunch of nonsense data to the stack (we choose the value to minimize error)
            for (int i = 0; i < 100; i++)
            {
                evm.ExecutionState.Stack.Push(1);
            }

            foreach (InstructionOpcode opcode in opcodes)
            {
                // Obtain our instruction.
                var opcodeDescriptor = opcode.GetDescriptor();
                var instruction = opcodeDescriptor.GetInstructionImplementation(evm);

                // Backup our stack size
                int stackSize = evm.ExecutionState.Stack.Count;

                try
                {
                    // Execute.
                    instruction.Execute();
                }
                catch { continue; }
                Assert.Equal(stackSize + ((int)opcodeDescriptor.ItemsAddedToStack - (int)opcodeDescriptor.ItemsRemovedFromStack), evm.ExecutionState.Stack.Count);
            }
        }
    }
}
