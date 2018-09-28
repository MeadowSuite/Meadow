using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.Exceptions;
using Meadow.EVM.EVM.Execution;
using Meadow.EVM.Configuration;

namespace Meadow.EVM.EVM.Instructions.System_Operations
{
    public class InstructionLog : InstructionBase
    {
        #region Constructors
        /// <summary>
        /// Our default constructor, reads the opcode/operand information from the provided stream.
        /// </summary>
        public InstructionLog(MeadowEVM evm) : base(evm) { }
        #endregion

        #region Functions
        public override void Execute()
        {
            // Obtain our log memory values
            BigInteger logMemoryStart = Stack.Pop();
            BigInteger logMemorySize = Stack.Pop();

            // We charge gas for every byte of memory we log
            GasState.Deduct(logMemorySize * GasDefinitions.GAS_LOG_BYTE);

            // Determine how many topics to read and pop them off the stack.
            int topicCount = (Opcode - InstructionOpcode.LOG0);
            List<BigInteger> topics = new List<BigInteger>();
            for (int i = 0; i < topicCount; i++)
            {
                topics.Add(Stack.Pop());
            }

            // If we're in a static context, we can't log
            if (Message.IsStatic)
            {
                throw new EVMException($"{Opcode.ToString()} instruction cannot execute in a static context!");
            }

            // Read our log memory data
            byte[] logMemoryData = Memory.ReadBytes((long)logMemoryStart, (int)logMemorySize);

            // Log our instruction
            EVM.State.Log(new Data_Types.Transactions.Log(Message.To, topics, logMemoryData));
        }
        #endregion
    }
}
