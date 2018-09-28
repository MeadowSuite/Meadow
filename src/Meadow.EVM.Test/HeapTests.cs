using Meadow.EVM.Data_Types.Trees;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Meadow.EVM.Test
{
    public class HeapTests
    {
        [Fact]
        public void MaxHeapInts()
        {
            // Create an array of integers and add them to our heap.
            Heap<int> intHeap = new Heap<int>(HeapOrder.Max);
            Random random = new Random();
            int[] array = new int[21];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = random.Next(int.MinValue, int.MaxValue);
                intHeap.Push(array[i]);
            }

            // Next we sort our array.
            Array.Sort(array);
            Array.Reverse(array);

            // Now we compare all items
            for (int i = 0; i < array.Length; i++)
            {
                Assert.Equal(array[i], intHeap.Pop());
            }

            // Verify our heap has no more items
            Assert.Equal(0, intHeap.Count);
        }

        [Fact]
        public void MinHeapInts()
        {
            // Create an array of integers and add them to our heap.
            Heap<int> intHeap = new Heap<int>(HeapOrder.Min);
            Random random = new Random();
            int[] array = new int[21];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = random.Next(int.MinValue, int.MaxValue);
                intHeap.Push(array[i]);
            }

            // Next we sort our array.
            Array.Sort(array);

            // Now we compare all items
            for (int i = 0; i < array.Length; i++)
            {
                Assert.Equal(array[i], intHeap.Pop());
            }

            // Verify our heap has no more items
            Assert.Equal(0, intHeap.Count);
        }

        [Fact]
        public void MaxHeapCustomCompare()
        {
            // Create an array of integers and add them to our heap. We use a custom compare function which puts even numbers first.
            Heap<int> intHeap = new Heap<int>(HeapOrder.Min, new Heap<int>.CompareFunction(
                (int first, int second) => 
                {
                    // Even numbers come first, otherwise it's normal comparison.

                    bool firstEven = first % 2 == 0;
                    bool secondEven = second % 2 == 0;
                    if (firstEven && !secondEven)
                    {
                        return -1;
                    }
                    else if (!firstEven && secondEven)
                    {
                        return 1;
                    }
                    else
                    {
                        return first.CompareTo(second);
                    }
                }));


            // Generate random numbers and add them to our list and heap.
            Random random = new Random();
            int count = 21;
            List<int> items = new List<int>();
            for (int i = 0; i < count; i++)
            {
                items.Add(random.Next(int.MinValue, int.MaxValue));
                intHeap.Push(items[i]);
            }

            // Sort our array
            items.Sort();

            // Now while we have items, we look for all even in order, then all odd in order.
            bool lookingForEvens = true;
            while (items.Count > 0)
            {
                int itemIndex = -1;
                for (int i = 0; i < items.Count; i++)
                {
                    if (lookingForEvens == (items[i] % 2 == 0))
                    {
                        itemIndex = i;
                        break;
                    }
                }

                // If our item index is -1, we stop looking for evens
                if (itemIndex == -1)
                {
                    lookingForEvens = false;
                    continue;
                }

                // Otherwise we grab the item and verify it to our heap item
                int item = items[itemIndex];
                items.RemoveAt(itemIndex);

                Assert.Equal(item, intHeap.Pop());
            }
        }

        [Fact]
        public void HeapEqualGivesEarlierQueued()
        {
            // In this test we see if equal items on the heap always returns the earlier item.

            // We test for both heap orders.
            HeapOrder[] heapOrders = new HeapOrder[] { HeapOrder.Min, HeapOrder.Max };

            // For each heap order
            foreach (HeapOrder heapOrder in heapOrders)
            {
                // Create our heap with the given order
                Heap<int> intHeap = new Heap<int>(heapOrder, new Heap<int>.CompareFunction(
                    (int first, int second) =>
                    {
                        // We'll return that everything is equal.
                        return 0;
                    }));


                // Generate random numbers and add them to our list and heap.
                Random random = new Random();
                int count = 20;
                List<int> items = new List<int>();
                for (int i = 0; i < count; i++)
                {
                    items.Add(random.Next(int.MinValue, int.MaxValue));
                    intHeap.Push(items[i]);
                }

                // Now for each item in our list, we make sure the order is the same.
                while (items.Count > 0)
                {
                    Assert.Equal(items[0], intHeap.Pop());
                    items.RemoveAt(0);
                }

                Assert.Empty(items);
                Assert.Equal(0, intHeap.Count);
            }
        }
    }
}
