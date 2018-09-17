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
using Meadow.EVM.Configuration;

namespace Meadow.EVM.EVM.Instructions.Control_Flow_and_IO
{
    public class InstructionStorageStore : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionStorageStore(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain our key and value
            BigInteger key = Stack.Pop();
            BigInteger value = Stack.Pop();

            // Determine our execution context is valid.
            if (Message.IsStatic)
            {
                throw new EVMException($"{Opcode.ToString()} cannot be executed within a static execution context.");
            }

            // Gas is calculated depending on the type of action that we're doing right now (adding, modifying, deleting). So we try to obtain the value first to find out it's state.
            BigInteger currentStorageValue = EVM.State.GetStorageData(Message.To, key);

            // If this value wasn't stored (none existing entries are zeroes, setting to zero removes as well)
            if (currentStorageValue == 0)
            {
                // If we are adding one now, charge for it, otherwise if it didn't change, we charge the modify amount.
                if (value != 0)
                {
                    GasState.Deduct(GasDefinitions.GAS_SSTORE_ADD);
                }
                else
                {
                    GasState.Deduct(GasDefinitions.GAS_SSTORE_MODIFY);
                }
            }
            else
            {
                // The current value was stored, if this new one will be too, then we charge the modifying amount, otherwise the delete amount.
                if (value != 0)
                {
                    GasState.Deduct(GasDefinitions.GAS_SSTORE_MODIFY);
                }
                else
                {
                    GasState.Deduct(GasDefinitions.GAS_SSTORE_DELETE);
                    EVM.State.AddGasRefund(GasDefinitions.GAS_SSTORE_REFUND);
                }
            }

            // Set our storage data
            EVM.State.SetStorageData(Message.To, key, value);
        }
        #endregion
    }
}
