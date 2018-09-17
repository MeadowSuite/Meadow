
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Data_Types.Trees
{
    /// <summary>
    /// Binary heap implementation (priority queue or min/max heap) which pushes and pops items with complexity O(log n). This implementation uses first-in, first-out ("FIFO") if priority is equal to another.
    /// </summary>
    /// <typeparam name="T">The generic type of items we will be adding to the heap.</typeparam>
    public class Heap<T>
    {
        #region Delegates
        /// <summary>
        /// Defines the function prototype for a comparison function which acts as CompareTo does, between two items, in order to sort the heap.
        /// </summary>
        /// <param name="first">The primary item to compare to another and return the status of.</param>
        /// <param name="second">The other item to compare the primary item to and return the status of.</param>
        /// <returns>Returns greater-than zero if first is larger than second, zero if they are equal, or less-than zero if the first is smaller than the second.</returns>
        public delegate int CompareFunction(T first, T second);
        #endregion

        #region Fields
        private ulong _currentNumber;
        /// <summary>
        /// Represents the internal item list which we order and treat as our heap.
        /// </summary>
        private List<(ulong number, T value)> _internalList;
        /// <summary>
        /// Represents an optional compare function we can call to compare heap items instead of the item's CompareTo function.
        /// </summary>
        private CompareFunction _compareFunction;
        #endregion

        #region Properties
        /// <summary>
        /// Represents the order of the data in the heap (indicates whether this is a min-heap or a max-heap).
        /// </summary>
        public HeapOrder Order { get; private set; }
        /// <summary>
        /// Indicates the count of items in this heap.
        /// </summary>
        public int Count
        {
            get
            {
                return _internalList.Count;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Our default constructor, sets the heap order and initializes any fields/properies for this heap instance.
        /// </summary>
        /// <param name="order">The order of our heap, indicating whether it is a min-heap or a max-heap.</param>
        /// <param name="compareFunction">An optional compare function which overrides the heap item's CompareTo function.</param>
        public Heap(HeapOrder order = HeapOrder.Max, CompareFunction compareFunction = null)
        {
            // Set our heap order
            Order = order;

            // Initialize our internal list.
            _internalList = new List<(ulong number, T value)>();

            // Set our compare function
            _compareFunction = compareFunction ?? DefaultCompareMethod;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Pushes an item into the heap.
        /// </summary>
        /// <param name="value">The item to push into the heap.</param>
        public void Push(T value)
        {
            // We add our value to the heap
            _internalList.Add((_currentNumber, value));

            // Sort our added node in relation to its parents.
            SortUpwards(Count - 1);

            // Increment our current number
            _currentNumber++;
        }

        /// <summary>
        /// Obtains the root/top most item of the heap, which is either the minimum or maximum of the heap, depending on heap order.
        /// </summary>
        /// <returns>Returns the root/top most item of the heap, which is either the minimum or maximum of the heap, depending on heap order.</returns>
        public T Peek()
        {
            // If our internal list length is 0, we return null
            if (_internalList.Count == 0)
            {
                return default;
            }

            // Otherwise we return our first item without popping it.
            return _internalList[0].value;
        }

        /// <summary>
        /// Obtains the root/top most item and removes it from the heap, which is either the minimum or maximum of the heap, depending on heap order.
        /// </summary>
        /// <returns>Removes and returns the root/top most item of the heap, which is either the minimum or maximum of the heap, depending on heap order.</returns>
        public T Pop()
        {
            // If our internal list length is 0, we return null
            if (_internalList.Count == 0)
            {
                return default;
            }

            // We obtain the item off of the top of the heap (either min or max)
            T oldRoot = _internalList[0].value;

            // Next we swap our last item into our first position (to keep position of other keys, but force a re-sort and rebalance)
            int lastIndex = _internalList.Count - 1;
            _internalList[0] = _internalList[lastIndex];
            _internalList.RemoveAt(lastIndex);

            // We sort downward from the new root
            SortDownwards(0);

            // And we return our old root
            return oldRoot;
        }

        /// <summary>
        /// Default function for a comparison function which acts as CompareTo does, between two items, in order to sort the heap.
        /// </summary>
        /// <param name="first">The first item to compare.</param>
        /// <param name="second">The second item to compare.</param>
        /// <returns>Returns greater-than zero if first is larger than second, zero if they are equal, or less-than zero if the first is smaller than the second.</returns>
        private int DefaultCompareMethod(T first, T second)
        {
            // Use our default compare method if it's an IComparable object.
            if (first is IComparable)
            {
                return ((IComparable)first).CompareTo(second);
            }

            // Otherwise we can just compare our hash codes.
            int firstHashCode = first.GetHashCode();
            int secondHashCode = second.GetHashCode();

            return firstHashCode - secondHashCode;
        }

        /// <summary>
        /// The function used to compare two items in our heap. Acts as CompareTo would.
        /// </summary>
        /// <param name="first">The first item in our heap to compare.</param>
        /// <param name="second">The second item in our heap to compare.</param>
        /// <returns>Returns greater-than zero if first is larger than second, zero if they are equal, or less-than zero if the first is smaller than the second.</returns>
        private int CompareItems((ulong number, T value) first, (ulong number, T value) second)
        {
            // Obtain our comparison result from comparing the two values.
            int compareResult = _compareFunction(first.value, second.value);
            
            // If the objects are equal, we take the one that was queued first.
            if (compareResult == 0)
            {
                if (first.number > second.number)
                {
                    return Order == HeapOrder.Min ? 1 : -1;
                }
                else if (first.number < second.number)
                {
                    return Order == HeapOrder.Min ? -1 : 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                // Otherwise we simply return the result
                return compareResult;
            }
        }

        /// <summary>
        /// Swaps two items in the internal heap array at the provided indexes.
        /// </summary>
        /// <param name="firstIndex">The index of the first item in our internal heap array to swap with another.</param>
        /// <param name="secondIndex">The index of the second item in our internal heap array to swap with another.</param>
        private void Swap(int firstIndex, int secondIndex)
        {
            // Back up our first item to swap
            (ulong number, T value) first = _internalList[firstIndex];

            // Swap our items
            _internalList[firstIndex] = _internalList[secondIndex];
            _internalList[secondIndex] = first;
        }

        /// <summary>
        /// Checks an item is sorted in relation to its parent, and if not, reorders them recursively until the tree branch is ordered.
        /// </summary>
        /// <param name="index">The index of the heap item in our internal array to check ordering with its parents recursively.</param>
        private void SortUpwards(int index)
        {
            // If our index is the root of the tree, we stop
            if (index == 0)
            {
                return;
            }

            // Obtain our parent index
            int parentIndex = (index - 1) / 2;

            // Compare the parent and the child, and determine if we should move.
            int compareResult = CompareItems(_internalList[parentIndex], _internalList[index]);

            // We only swap if we meet our ordering conditions.
            bool swapping = (compareResult > 0 && Order == HeapOrder.Min) | (compareResult < 0 && Order == HeapOrder.Max);
            if (!swapping)
            {
                return;
            }

            // Swap our parent and child.
            Swap(parentIndex, index);

            // Continue reordering heap items up the tree until we hit the root, or we satisfy sorting.
            SortUpwards(parentIndex);
        }

        /// <summary>
        /// Checks an item is sorted in relation to its children, and if not, reorders them recursively until the three branches are ordered.
        /// </summary>
        /// <param name="index">The index of the heap item in our internal array to check ordering with its children recursively.</param>
        private void SortDownwards(int index)
        {
            // Obtain our left and right index.
            int leftChildIndex = (index * 2) + 1;
            int rightChildIndex = leftChildIndex + 1;

            // If the left node index (earlier than right) is past the last leaf, we stop.
            if (leftChildIndex >= Count)
            {
                return;
            }

            // Determine which child is more desirable to have higher up (min or max).
            int targetChildIndex = -1;

            // If the right child doesn't exist, we choose the left.
            int compareResult;
            if (rightChildIndex >= Count)
            {
                targetChildIndex = leftChildIndex;
            }
            else
            {
                // If both left and right exist, we take the minimum if it's a min-heap, or maximum if it's a max-heap. If they're equal, we prefer the left (earlier) side.+
                compareResult = CompareItems(_internalList[leftChildIndex], _internalList[rightChildIndex]);
                if ((compareResult <= 0 && Order == HeapOrder.Min) || (compareResult >= 0 && Order == HeapOrder.Max))
                {
                    targetChildIndex = leftChildIndex;
                }
                else
                {
                    targetChildIndex = rightChildIndex;
                }
            }

            // Compare the parent and the child, and determine if we should move.
            compareResult = CompareItems(_internalList[index], _internalList[targetChildIndex]);
            bool swapping = (compareResult > 0 && Order == HeapOrder.Min) | (compareResult < 0 && Order == HeapOrder.Max);
            if (!swapping)
            {
                return;
            }

            // Swap our parent and child.
            Swap(index, targetChildIndex);

            // Continue reordering heap items down the tree until we hit the leaf, or satisfy sorting.
            SortDownwards(targetChildIndex);
        }
        #endregion
    }

    #region Enums
    /// <summary>
    /// The heap order which indicates if the heap is a min-heap or a max-heap.
    /// </summary>
    public enum HeapOrder
    {
        Min,
        Max
    }
    #endregion
}
