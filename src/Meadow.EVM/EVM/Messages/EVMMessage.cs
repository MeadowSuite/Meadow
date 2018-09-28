using Meadow.EVM.Data_Types.Addressing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Messages
{
    /// <summary>
    /// Represents the internal message calls which the Ethereum virtual machine initiates execution/calls based off of. A structure which represents call data for contracts. The initial call is usually derived from a transaction.
    /// </summary>
    public class EVMMessage
    {
        #region Properties
        /// <summary>
        /// The sender of the message.
        /// </summary>
        public Address Sender { get; }
        /// <summary>
        /// The recipient of the message.
        /// </summary>
        public Address To { get; set; }
        /// <summary>
        /// The deposited value by the instruction/transaction responsible for this call, or amount to transfer to an account.
        /// </summary>
        public BigInteger Value { get; }
        /// <summary>
        /// The amount of gas sent with the transaction.
        /// </summary>
        public BigInteger Gas { get; }
        /// <summary>
        /// An optional data parameter.
        /// </summary>
        public byte[] Data { get; set; }
        /// <summary>
        /// Call stack depth. This should be incremented with one for every call we go deeper into.
        /// </summary>
        public BigInteger Depth { get; }
        /// <summary>
        /// The ethereum address where the code resides (not to be confused with code offset)
        /// </summary>
        public Address CodeAddress { get; }
        /// <summary>
        /// Indicates value from the "Sender" should be transfered to the "To" address. The amount to send is in "Value".
        /// </summary>
        public bool IsTransferringValue { get; }
        /// <summary>
        /// Indicates if this message is as a result of a STATICCALL. In static context, any subcalls will also be static, and no state changing will be allowed, including non-zero value calls, creates, SSTORE, SELFDESTRUCT, etc.
        /// </summary>
        public bool IsStatic { get; }
        #endregion

        #region Constructor
        /// <summary>
        /// Our default constructor, sets all properties for our message.
        /// </summary>
        /// <param name="sender">The origin of the message.</param>
        /// <param name="to">The destination of the message.</param>
        /// <param name="value">The amount to transfer, or a value passed by the caller to a callee.</param>
        /// <param name="gas">The amount of gas sent with this transaction to execute on.</param>
        /// <param name="data">The message's call data.</param>
        /// <param name="depth">The depth of the call, meaning how many message calls deep we are.</param>
        /// <param name="codeAddress">The address where the code resides to execute.</param>
        /// <param name="transfersValue">Indicates whether we are transferring a balance amount between accounts.</param>
        /// <param name="isStatic">Indicates if the call is in a static context.</param>
        public EVMMessage(Address sender, Address to, BigInteger value, BigInteger gas, byte[] data, BigInteger depth, Address codeAddress, bool transfersValue, bool isStatic)
        {
            // Set all of our properties.
            Sender = sender;
            To = to;
            Value = value;
            Gas = gas;
            Data = data;
            Depth = depth;
            CodeAddress = codeAddress;
            IsTransferringValue = transfersValue;
            IsStatic = isStatic;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Predits the address of the code post-deployment, regardless of whether are we deploying now or not.
        /// </summary>
        public Address GetDeployedCodeAddress()
        {
            // If we have a valid code address, return it, otherwise return our To address.
            if (CodeAddress != Address.CREATE_CONTRACT_ADDRESS)
            {
                return CodeAddress;
            }
            else
            {
                return To;
            }
        }
        #endregion
    }
}
