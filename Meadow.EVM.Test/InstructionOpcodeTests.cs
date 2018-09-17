using Meadow.EVM.Configuration;
using Meadow.EVM.EVM.Instructions;
using System;
using Xunit;

namespace Meadow.EVM.Test
{
    public class InstructionOpcodeTests
    {
        /// <summary>
        /// All opcode descriptors should reference an ImplementationClassType, the type of the class where the instruction is implemented.
        /// </summary>
        [Fact]
        public void TestImplementationClassTypes()
        {
            // First obtain all enum options for opcodes.
            InstructionOpcode[] opcodes = (InstructionOpcode[])Enum.GetValues(typeof(InstructionOpcode));

            // Loop for every opcode.
            foreach (InstructionOpcode opcode in opcodes)
            {
                Assert.NotNull(opcode.GetDescriptor().ImplementationClassType);
            }
        }

        /// <summary>
        /// The mnemonic in the opcode descriptor should have the same name as the enum option it belongs to.
        /// NOTE: This is a test of incompleteness/inaccuracy, however any mnemonic/name may be used without affecting EVM functionality.
        /// </summary>
        [Fact]
        public void TestMnemonics()
        {
            // First obtain all enum options for opcodes.
            InstructionOpcode[] opcodes = (InstructionOpcode[])Enum.GetValues(typeof(InstructionOpcode));

            // Loop for every opcode.
            foreach (InstructionOpcode opcode in opcodes)
            {
                Assert.Equal(opcode.ToString(), opcode.GetDescriptor().Mnemonic);
            }
        }

        /// <summary>
        /// Opcode descriptions should exist for every opcode. 
        /// NOTE: This is a test of incompleteness, however any description may be used without affecting EVM functionality.
        /// </summary>
        [Fact]
        public void TestDescriptions()
        {
            // First obtain all enum options for opcodes.
            InstructionOpcode[] opcodes = (InstructionOpcode[])Enum.GetValues(typeof(InstructionOpcode));

            // Loop for every opcode.
            foreach (InstructionOpcode opcode in opcodes)
            {
                Assert.NotNull(opcode.GetDescriptor().Description);
            }
        }

        /// <summary>
        /// All opcodes should have a base gas cost attribute for at least some version of Ethereum (should be starting from where it was introduced, and include updates)
        /// </summary>
        [Fact]
        public void TestBaseGasCosts()
        {
            // First obtain all enum options for opcodes.
            InstructionOpcode[] opcodes = (InstructionOpcode[])Enum.GetValues(typeof(InstructionOpcode));

            // Loop for every opcode.
            foreach (InstructionOpcode opcode in opcodes)
            {
                var baseGasCosts = opcode.GetBaseGasCosts();
                Assert.NotNull(baseGasCosts);
                Assert.NotEmpty(baseGasCosts);
            }
        }

        /// <summary>
        /// All opcodes should have a base gas cost attribute for at least some version of Ethereum (should be starting from where it was introduced, and include updates)
        /// </summary>
        [Fact]
        public void TestBaseGasCostUpdates()
        {
            // Make static assertions about updates made during Tangerine Whistle release.
            EthereumRelease[] releases = (EthereumRelease[])Enum.GetValues(typeof(EthereumRelease));
            foreach (EthereumRelease release in releases)
            {
                if (release < EthereumRelease.TangerineWhistle)
                {
                    Assert.Equal<uint>(20, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.BALANCE));
                    Assert.Equal<uint>(20, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.EXTCODESIZE));
                    Assert.Equal<uint>(20, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.EXTCODECOPY));
                    Assert.Equal<uint>(50, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.SLOAD));
                    Assert.Equal<uint>(40, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.CALL));
                    Assert.Equal<uint>(40, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.CALLCODE));

                    // DELEGATE CALL WAS INTRODUCED IN HOMESTEAD WITH 40 BASE GAS, NULL BEFORE.
                    if (release >= EthereumRelease.Homestead)
                    {
                        Assert.Equal<uint>(40, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.DELEGATECALL));
                    }
                    else
                    {
                        Assert.Null(GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.DELEGATECALL));
                    }

                    // STATIC CALL WAS INTRODUCED IN BYZANTIUM WITH 700 BASE GAS, NULL BEFORE
                    if (release >= EthereumRelease.Byzantium)
                    {
                        Assert.Equal<uint>(700, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.STATICCALL));
                    }
                    else
                    {
                        Assert.Null(GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.STATICCALL));
                    }

                    Assert.Equal<uint>(0, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.SELFDESTRUCT));
                }
                else
                {
                    Assert.Equal<uint>(400, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.BALANCE));
                    Assert.Equal<uint>(700, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.EXTCODESIZE));
                    Assert.Equal<uint>(700, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.EXTCODECOPY));
                    Assert.Equal<uint>(200, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.SLOAD));
                    Assert.Equal<uint>(700, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.CALL));
                    Assert.Equal<uint>(700, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.CALLCODE));
                    Assert.Equal<uint>(700, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.DELEGATECALL));
                    Assert.Equal<uint>(5000, (uint)GasDefinitions.GetInstructionBaseGasCost(release, InstructionOpcode.SELFDESTRUCT));
                }
            }
        }
    }
}
