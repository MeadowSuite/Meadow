using Meadow.Core.Utils;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.Block_Information
{
    public class InstructionBlockHash : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionBlockHash(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain the current block number, and the block number for the block we want the hash for.
            BigInteger currentBlockNumber = EVM.State.CurrentBlock.Header.BlockNumber;
            BigInteger targetBlockNumber = Stack.Pop();
            BigInteger previousHeaderIndex = currentBlockNumber - targetBlockNumber - 1;

            // We only cache a finite amount of block headers, currently this is 256. 
            // NOTE: Although it's defined in our configuration, we hard code it here as some official Ethereum implementations do, 
            // instead of being dependent on configuration. This way, if it were to change with a fork, we still remain backward compatible here
            // and can simply just add a case.
            if (previousHeaderIndex < 0 || previousHeaderIndex > 255)
            {
                // We couldn't obtain the block hash, we return zero.
                Stack.Push(0);
            }
            else
            {
                // Obtain the hash of this previous block and push it.
                Stack.Push(BigIntegerConverter.GetBigInteger(EVM.State.PreviousHeaders[(int)previousHeaderIndex].GetHash()));

                // Verify there was not some error with block numbers mismatches on the previous header we obtained.
                if (targetBlockNumber != EVM.State.PreviousHeaders[(int)previousHeaderIndex].BlockNumber)
                {
                    throw new Exception($"BlockNumber mismatch when performing a {Opcode.ToString()} instruction in the EVM!");
                }
            }
        }
        #endregion
    }

}
