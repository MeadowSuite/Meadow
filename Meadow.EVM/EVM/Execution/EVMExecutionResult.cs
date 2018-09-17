using Meadow.EVM.EVM.Definitions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Execution
{
    /// <summary>
    /// Represents the concluding result of execution in the Ethereum Virtual Machine.
    /// </summary>
    public class EVMExecutionResult
    {
        #region Properties
        public MeadowEVM EVM { get;  }
        /// <summary>
        /// Represents data which was returned upon execution of the VM.
        /// </summary>
        public Memory<byte> ReturnData { get;  }
        /// <summary>
        /// Represents the amount of gas remaining after execution.
        /// </summary>
        public BigInteger RemainingGas { get;  }
        /// <summary>
        /// Indicates whether or not all changes should be reverted.
        /// </summary>
        public bool Succeeded { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// Our default constructor, sets all properties of our execution result. Using this constructor returns the remaining gas in the EVM instance.
        /// </summary>
        /// <param name="evm">The EVM instance which the result is being returned for, and from which we should derive remaining gas.</param>
        /// <param name="returnData">Represents data which was returned upon execution of the VM.</param>
        /// <param name="succeeded">Indicates whether or not all changes should be reverted.</param>
        public EVMExecutionResult(MeadowEVM evm, Memory<byte> returnData, bool succeeded) : this(evm, returnData, evm.GasState.Gas, succeeded) { }
        /// <summary>
        /// Our default constructor, sets all properties of our execution result.
        /// </summary>
        /// <param name="evm">The EVM instance which the result is being returned for.</param>
        /// <param name="returnData">Represents data which was returned upon execution of the VM.</param>
        /// <param name="remainingGas">Represents the amount of gas remaining after execution.</param>
        /// <param name="succeeded">Indicates whether or not all changes should be reverted.</param>
        public EVMExecutionResult(MeadowEVM evm, Memory<byte> returnData, BigInteger remainingGas, bool succeeded)
        {
            EVM = evm;
            ReturnData = returnData;
            RemainingGas = remainingGas;
            Succeeded = succeeded;
        }
        #endregion
    }
}
