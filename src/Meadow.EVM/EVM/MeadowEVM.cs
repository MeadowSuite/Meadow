using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types.State;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Instructions;
using Meadow.EVM.EVM.Messages;
using Meadow.EVM.EVM.Precompiles;
using Meadow.EVM.EVM.Execution;
using System;
using System.IO;
using System.Numerics;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Exceptions;
using Meadow.EVM.Debugging.Coverage;
using System.Runtime.CompilerServices;
using static Meadow.EVM.Debugging.Coverage.CodeCoverage;

namespace Meadow.EVM
{
    public class MeadowEVM
    {
        #region Properties
        public Memory<byte> Code { get; set; }
        public State State { get; set; }
        public EVMMessage Message { get; set; }
        public EVMExecutionState ExecutionState { get; set; }
        public EVMGasState GasState { get; set; }
        public EthereumRelease Version
        {
            get
            {
                return State.Configuration.Version;
            }
        }

        public EthereumChainID ChainID
        {
            get
            {
                return State.Configuration.ChainID;
            }
        }

        public CoverageMap CoverageMap { get; set; }
        #endregion

        #region Constructor
        public MeadowEVM()
        {

        }
        #endregion

        #region Functions
        public static EVMExecutionResult CreateContract(State state, EVMMessage message)
        {
            // If this message to create didn't come from the transaction origin, we increment nonce
            if (state.CurrentTransaction.GetSenderAddress() != message.Sender)
            {
                state.IncrementNonce(message.Sender);
            }

            BigInteger newNonce = state.GetNonce(message.Sender) - 1;
            message.To = Address.MakeContractAddress(message.Sender, newNonce);

            // If we're past the byzantium fork, we want to make sure the nonce is 0 and there is no code (making sure the address we're creating doesn't already exist). Otherwise we fail out.
            if (state.Configuration.Version >= EthereumRelease.Byzantium)
            {
                byte[] existingCode = state.GetCodeSegment(message.To);
                if (state.GetNonce(message.To) > 0 || existingCode.Length > 0)
                {
                    return new EVMExecutionResult(null, null, 0, false);
                }
            }

            // If this is an existing account, remove existing values attached to it.
            BigInteger balance = state.GetBalance(message.To);
            if (balance > 0)
            {
                state.SetBalance(message.To, balance);
                state.SetNonce(message.To, 0);
                state.SetCodeSegment(message.To, Array.Empty<byte>());
            }

            // Obtain our code from our message data
            byte[] code = message.Data;

            // Set our message data to a blank array
            message.Data = Array.Empty<byte>();

            // Back up the state.
            StateSnapshot snapshot = state.Snapshot();

            // If spurious dragon version
            if (state.Configuration.Version >= EthereumRelease.SpuriousDragon)
            {
                state.SetNonce(message.To, 1);
            }
            else
            {
                state.SetNonce(message.To, 0);
            }

            // Execute our message
            EVMExecutionResult result = Execute(state, message, code);

            // If we should revert
            if (!result.Succeeded)
            {
                // Revert our changes
                state.Revert(snapshot);

                // Return our execution result
                return result;
            }
            else
            {
                // If we have no return data, our return data is the To address.
                if (result.ReturnData.Length == 0)
                {
                    // Record an exception here (although this is technically not an exception, it is acceptable behavior, but in any real world case, this is undesirable, so we warn of it).
                    state.Configuration.DebugConfiguration?.RecordException(new Exception("Contract deployment ended up deploying a contract which is zero bytes in size."), false);

                    // Return early to avoid processing more code.
                    return new EVMExecutionResult(result.EVM, message.To.ToByteArray(), result.RemainingGas, true);
                }

                // Obtain our code
                code = result.ReturnData.ToArray();

                // Calculate cost based off every byte in our contract
                BigInteger remainingGas = result.RemainingGas;
                BigInteger extraGasCost = result.ReturnData.Length * GasDefinitions.GAS_CONTRACT_BYTE;

                // Verify we have enough gas to create the contract, and we pass the the size constraint for contracts introduced in spurious dragon.
                bool passSizeContraint = (state.Configuration.Version < EthereumRelease.SpuriousDragon || code.Length <= EVMDefinitions.MAX_CONTRACT_SIZE);

                // Allow contract to be over the size limit if the debug/testing option for it has been set
                if (!passSizeContraint && state.Configuration.DebugConfiguration.IsContractSizeCheckDisabled)
                {
                    passSizeContraint = true;
                }

                if (result.RemainingGas < extraGasCost || !passSizeContraint)
                {
                    // Store our code length
                    var codeSize = code.Length;

                    // Set our code array as blank
                    code = Array.Empty<byte>();

                    // If we are past homestead, we revert here.
                    if (state.Configuration.Version >= EthereumRelease.Homestead)
                    {
                        // Report an exception here.
                        string exceptionMessage = null;
                        if (!passSizeContraint)
                        {
                            exceptionMessage = $"Out of gas: Contract size of {codeSize} bytes exceeds the maximum contract size of {EVMDefinitions.MAX_CONTRACT_SIZE} bytes.";
                        }
                        else
                        {
                            exceptionMessage = $"Out of gas: Not enough gas to pay for the cost-per-byte of deployed contract. Gas: {result.RemainingGas} / Cost: {extraGasCost}";
                        }

                        // Record an exception here.
                        state.Configuration.DebugConfiguration?.RecordException(new Exception(exceptionMessage), false);

                        // Revert our changes
                        state.Revert(snapshot);

                        // Return our execution result
                        return new EVMExecutionResult(result.EVM, null, 0, false);
                    }
                }
                else
                {
                    // We could pay for the creation, remove the gas cost from our remaining gas.
                    remainingGas -= extraGasCost;
                }

                // Set our code segment.
                state.SetCodeSegment(message.To, code);

                // Return our result
                return new EVMExecutionResult(result.EVM, message.To.ToByteArray(), remainingGas, true);
            }
        }

        public static EVMExecutionResult Execute(State state, EVMMessage message)
        {
            // Obtain the code from our account if it's not a precompile address we're calling.
            byte[] codeSegment = null;
            if (!EVMPrecompiles.IsPrecompileAddress(message.CodeAddress))
            {
                codeSegment = state.GetCodeSegment(message.CodeAddress);
            }

            // Execute on it
            return Execute(state, message, codeSegment);
        }

        public static EVMExecutionResult Execute(State state, EVMMessage message, Memory<byte> code)
        {
            // Backup the state.
            StateSnapshot snapshot = state.Snapshot();

            // If we're supposed to transfer value, then transfer it. If our balance is insufficient, we exit early and say our transaction succeeded.
            if (message.IsTransferringValue && !state.TransferBalance(message.Sender, message.To, message.Value))
            {
                return new EVMExecutionResult(null, null, message.Gas, true);
            }

            // Initialize an EVM instance
            MeadowEVM evm = new MeadowEVM();

            // Otherwise, we continue with our VM execution.
            evm.ExecuteInternal(state, message, code);

            // Restore all states here if our results say we failed.
            if (!evm.ExecutionState.Result.Succeeded)
            {
                // Revert any changes since we didn't succeed.
                state.Revert(snapshot);
            }

            // Return our evm after it has successfully executed.
            return evm.ExecutionState.Result;
        }

        private void ExecuteInternal(State state, EVMMessage message)
        {
            // Obtain the code from our account if it's not a precompile address we're calling.
            byte[] codeSegment = null;
            if (!EVMPrecompiles.IsPrecompileAddress(message.CodeAddress))
            {
                codeSegment = state.GetCodeSegment(message.CodeAddress);
            }

            // Execute on it
            ExecuteInternal(state, message, codeSegment);
        }

        private void ExecuteInternal(State state, EVMMessage message, Memory<byte> code)
        {
            // Set our current state and message
            State = state;
            Message = message;

            // Create a fresh execuction state.
            ExecutionState = new EVMExecutionState(this);
            GasState = new EVMGasState(Message.Gas);
            Code = code;

            // Record our call's execution start
            State.Configuration.DebugConfiguration.RecordExecutionStart(this);

            // We'll want to wrap our actual execution in a try block to catch any internal VM exceptions specifically
            bool isPrecompile = EVMPrecompiles.IsPrecompileAddress(Message.CodeAddress);
            bool threwException = false;
            try
            {
                // Check if address is in precompiles. If so, we execute precompile instead of the provided code.
                if (isPrecompile)
                {
                    // Execute our precompiled code.
                    EVMPrecompiles.ExecutePrecompile(this, Message.CodeAddress);
                }
                else
                {
                    // Register our code coverage for this contract (if we're not mining).
                    CoverageMap = State.Configuration.CodeCoverage.Register(Message, Code);

                    // Execute until we have an execution result.
                    while (ExecutionState.Result == null)
                    {
                        // Run another instruction.
                        Step();
                    }
                }
            }
            catch (Exception exception)
            {
                // Record our exception. If we're not a precompile, we mark it as being an in-contract-execution exception.
                State.Configuration.DebugConfiguration.RecordException(exception, true);

                // If our exception is an evm exception, we set our execution result.
                if (exception is EVMException)
                {
                    // An internal VM exception occurred, we'll want to return nothing, burn all the gas, and revert any changes.
                    ExecutionState.Result = new EVMExecutionResult(this, null, 0, false);
                }
                else
                {
                    // If it's any other type of exception, throw it.
                    throw;
                }

                threwException = true;
            }

            // If we didn't succeed, record an exception indicating we failed and will revert.
            if (State.Configuration.DebugConfiguration.ThrowExceptionOnFailResult && !ExecutionState.Result.Succeeded && !threwException)
            {
                State.Configuration.DebugConfiguration.RecordException(new Exception("Execution returned a failed result. REVERTING..."), true);
            }

            // Record our call's execution end
            State.Configuration.DebugConfiguration.RecordExecutionEnd(this);
        }

        /// <summary>
        /// Executes the next instruction located at the program counter, and advances it accordingly.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Step()
        {
            // Verify we're not at the end of the stream
            if (ExecutionState.PC >= Code.Length)
            {
                // If we reached the end, exit with the remainder of the gas and a success status.
                ExecutionState.Result = new EVMExecutionResult(this, null, true);
                return;
            }

            // Set our position back to the start of the instruction, grab our opcode and verify it.
            InstructionOpcode opcode = (InstructionOpcode)Code.Span[(int)ExecutionState.PC];

            // Obtain our base cost for this opcode. If we fail to, the instruction isn't implemented yet.
            uint? instructionBaseGasCost = GasDefinitions.GetInstructionBaseGasCost(State.Configuration.Version, opcode);
            if (instructionBaseGasCost == null)
            {
                throw new EVMException($"Invalid opcode {opcode.ToString()} read when executing!");
            }

            // If we just jumped, then this next opcode should be a JUMPDEST.
            if (ExecutionState.JumpedLastInstruction && opcode != InstructionOpcode.JUMPDEST)
            {
                throw new EVMException($"Invalid jump to offset {ExecutionState.PC} in code!");
            }

            // Obtain our instruction implementation for this opcode
            var opcodeDescriptor = opcode.GetDescriptor();
            InstructionBase instruction = opcodeDescriptor.GetInstructionImplementation(this);

            // Record our code coverage for this execution.
            CoverageMap?.RecordExecution(instruction.Offset, (ExecutionState.PC - instruction.Offset));

            // Record our instruction execution tracing
            if (State.Configuration.DebugConfiguration.IsTracing)
            {
                State.Configuration.DebugConfiguration.ExecutionTrace?.RecordExecution(this, instruction, GasState.Gas, (BigInteger)instructionBaseGasCost);
            }

            // Deduct base gas cost
            GasState.Deduct((BigInteger)instructionBaseGasCost);

            // Debug: Print out instruction execution information.
            //if (opcode == InstructionOpcode.JUMPDEST)
            //    Console.WriteLine($"\r\n---------------------------------------------------------------\r\n");
            //Console.WriteLine($"0x{instruction.Offset.ToString("X4")}: {instruction}");
            //Console.WriteLine($"Stack: {ExecutionState.Stack}");

            // Execute the instruction
            instruction.Execute();
        }
        #endregion
    }
}
