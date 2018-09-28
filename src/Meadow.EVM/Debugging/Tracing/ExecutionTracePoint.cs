using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Instructions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Debugging.Tracing
{
    public class ExecutionTracePoint
    {
        #region Properties
        /// <summary>
        /// The entire code segment being processed at this point. NULL if unchanged since last known value.
        /// </summary>
        public byte[] Code { get; set; }
        /// <summary>
        /// The address of the contract currently executing at this point. NULL if unchanged since last known value.
        /// </summary>
        public Address ContractAddress { get; set; }
        /// <summary>
        /// Indicates if the contract currently executing at this point is a deployed contract (true), or if it is in the process of deploying (false).
        /// </summary>
        public bool ContractDeployed { get; set; }
        /// <summary>
        /// Gas remaining prior to the instruction's execution.
        /// </summary>
        public BigInteger GasRemaining { get; set; }
        /// <summary>
        /// The amount of gas this instruction cost.
        /// </summary>
        public BigInteger GasCost { get; set; }
        /// <summary>
        /// The string representation of an opcode.
        /// </summary>
        public string Opcode { get; set; }
        /// <summary>
        /// The program counter prior to this instruction's execution.
        /// </summary>
        public uint PC { get; set; }
        /// <summary>
        /// Our Ethereum virtual machine call depth (not to be confused with high level function call depth such as solidity)
        /// </summary>
        public uint Depth { get; set; }
        /// <summary>
        /// A representation of the EVM memory prior to this instruction's execution. NULL if unchanged since last known value.
        /// </summary>
        public byte[] Memory { get; set; }
        /// <summary>
        /// A representation of the EVM stack prior to this instruction's execution.
        /// </summary>
        public byte[][] Stack { get; set; }
        /// <summary>
        /// Represents all key-value storage for the <see cref="EVM.Messages.EVMMessage.To"/> address at this point in execution.
        /// </summary>
        public Dictionary<Memory<byte>, byte[]> Storage { get; set; }
        #endregion
    }
}
