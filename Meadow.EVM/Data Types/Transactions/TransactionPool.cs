using Meadow.EVM.Data_Types.Trees;
using Meadow.EVM.EVM.Definitions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Data_Types.Transactions
{
    /// <summary>
    /// Represents the collection of transactions waiting to be added to a block/mined/executed.
    /// </summary>
    public class TransactionPool
    {
        #region Fields
        private Heap<Transaction> _queue;
        private Heap<Transaction> _rejected;
        #endregion

        #region Constructor
        public TransactionPool()
        {
            // We create a transaction heap with our given compare function.
            _queue = new Heap<Transaction>(HeapOrder.Max, new Heap<Transaction>.CompareFunction(TransactionCompareFunction));
            _rejected = new Heap<Transaction>(HeapOrder.Min, new Heap<Transaction>.CompareFunction(TransactionCompareFunction));
        }
        #endregion

        #region Properties
        /// <summary>
        /// The count of items we have in our transaction pool.
        /// </summary>
        public int Count
        {
            get
            {
                return _queue.Count + _rejected.Count;
            }
        }
        #endregion

        #region Functions
        /// <summary>
        /// The comparison function for our transactions in order to sort it in our heap, which sorts by gas price, otherwise queued status.
        /// </summary>
        /// <param name="first">The first item to compare from our heap.</param>
        /// <param name="second">The second item to compare from our heap.</param>
        /// <returns>Returns the item with the highest gas price, or if equal, the one which was queued first.</returns>
        private int TransactionCompareFunction(Transaction first, Transaction second)
        {
            // We compare by looking for greater start gas first, otherwise we fall back on earlier item index.
            if (first.StartGas > second.StartGas)
            {
                return 1;
            }
            else if (first.StartGas < second.StartGas)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Adds a transaction to our transaction pool, queuing it for mining/block/execution.
        /// </summary>
        /// <param name="transaction">The transaction to add to the transaction pool.</param>
        public void Push(Transaction transaction)
        {
            // Push our item onto our transaction heap.
            _queue.Push(transaction);
        }

        /// <summary>
        /// Obtains the most expensive from the transaction pool.
        /// </summary>
        /// <returns>Returns the most expensive transaction out of the transaction pool.</returns>
        public Transaction Pop()
        {
            // Weap our other pop function
            return Pop(0, EVMDefinitions.UINT256_MAX_VALUE);
        }

        /// <summary>
        /// Obtains the most expensive transaction that meets the given requirements from the transaction pool.
        /// </summary>
        /// <param name="maxStartGas">The maximum start gas the transaction to pop off should have. (Inclusive)</param>
        /// <param name="minGasPrice">The minimum gas price the trasanction to pop off should have. (Inclusive)</param>
        /// <returns>Returns a transaction that meets the criteria out of the transaction pool.</returns>
        public Transaction Pop(BigInteger minGasPrice, BigInteger maxStartGas)
        {
            // Add back any rejected transactions that meet our criteria first.
            while (_rejected.Count > 0 && maxStartGas >= _rejected.Peek().StartGas)
            {
                // Pop the item off the rejected heap and push it onto our transaction heap.
                Transaction item = _rejected.Pop();
                _queue.Push(item);
            }

            // Now we loop for either all transactions in the transaction heap to try and find one that satisfies our condition.
            while (_queue.Count > 0)
            {
                // Pop off a transaction
                Transaction item = _queue.Pop();

                // Check if it meets our requirements, otherwise we throw it on our overflow.
                if (item.StartGas > maxStartGas || item.GasPrice < minGasPrice)
                {
                    _rejected.Push(item);
                }
                else
                {
                    return item;
                }
            }

            // We popped all transactions and didn't find one that suited our needs.
            return null;
        }
        #endregion
    }
}
