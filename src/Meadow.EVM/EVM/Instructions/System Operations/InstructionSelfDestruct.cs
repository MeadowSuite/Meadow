using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types.Accounts;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.Exceptions;
using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.System_Operations
{
    public class InstructionSelfDestruct : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionSelfDestruct(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // If we're in a static context, we can't self destruct
            if (Message.IsStatic)
            {
                throw new EVMException($"{Opcode.ToString()} instruction cannot execute in a static context!");
            }

            // Obtain our address to
            Address to = Stack.Pop();
            BigInteger balanceMessageTo = EVM.State.GetBalance(Message.To);
            BigInteger balanceTo = EVM.State.GetBalance(to);

            // Calculate our extra gas costs for post-Tangerine Whistle.
            if (Version >= EthereumRelease.TangerineWhistle)
            {
                // If we had a balance to send (or are pre-spurious dragon which didn't care), and our account to send the balance to doesn't exist, charge for calling a new account.
                if (!EVM.State.ContainsAccount(to))
                {
                    if (balanceMessageTo > 0 || Version < EthereumRelease.SpuriousDragon)
                    {
                        GasState.Deduct(GasDefinitions.GAS_CALL_NEW_ACCOUNT);
                    }
                }
            }

            // Transfer our balance from our message recipient to
            EVM.State.SetBalance(to, balanceTo + balanceMessageTo);
            EVM.State.SetBalance(Message.To, 0);

            // Log our suicide in our state.
            EVM.State.QueueDeleteAccount(Message.To);

            // We'll want to return with no return data, our remaining gas, and indicating we don't wish to revert changes.
            Return(new EVMExecutionResult(EVM, null, true));
        }
        #endregion
    }
}
