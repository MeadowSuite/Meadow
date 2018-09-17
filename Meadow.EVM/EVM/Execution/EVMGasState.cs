using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.Exceptions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Execution
{
    public class EVMGasState
    {
        #region Properties
        /// <summary>
        /// Indicates the amount of gas that we had when we created this gas state.
        /// </summary>
        public BigInteger InitialGas { get; }
        /// <summary>
        /// Indicates the amount of gas one has remaining in the current state.
        /// </summary>
        public BigInteger Gas { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Our default constructor, sets the amount of gas we have to operate on.
        /// </summary>
        /// <param name="gas">The amount of gas we have to run computations.</param>
        public EVMGasState(BigInteger gas)
        {
            // Set our current gas amount
            InitialGas = gas;
            Gas = gas;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Throw an exception if we do not have enough Gas to handle the given amount.
        /// </summary>
        /// <param name="amount">The amount of gas we are checking if we have.</param>
        public void Check(BigInteger amount)
        {
            // Throw an exception if we don't have enough gas.
            if (Gas < amount)
            {
                throw new EVMException("Insufficient gas to execute further.");
            }
        }

        /// <summary>
        /// Deducts the given amount from our current gas.
        /// </summary>
        /// <param name="amount">The amount of gas to deduct from our current gas.</param>
        public void Deduct(BigInteger amount)
        {
            // Verify there was not an issue with deducting a negative amount of gas, we throw a generic exception instead of EVM, as this should be checked.
            if (amount < 0)
            {
                throw new Exception("Cannot deduct a negative amount of gas.");
            }

            // Check we have a valid amount.
            Check(amount);

            // Deduct our amount
            Gas -= amount;
        }

        /// <summary>
        /// Refunds the given amount of gas to our current gas.
        /// </summary>
        /// <param name="amount">The amount of gas to refund to our current gas.</param>
        public void Refund(BigInteger amount)
        {
            // Verify there was not an issue with rewarding a negative amount of gas, we throw a generic exception instead of EVM, as this should be checked.
            if (amount < 0)
            {
                throw new Exception("Cannot deduct a negative amount of gas.");
            }

            // Add our amount
            Gas += amount;
        }
        #endregion
    }
}
