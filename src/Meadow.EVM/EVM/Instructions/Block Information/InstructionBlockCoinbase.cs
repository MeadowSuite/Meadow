using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Instructions.Block_Information
{
    public class InstructionBlockCoinbase : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionBlockCoinbase(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // We'll push our coinbase address
            Stack.Push(EVM.State.CurrentBlock.Header.Coinbase);
        }
        #endregion
    }

}
