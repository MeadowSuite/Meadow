using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

public static class ArrayExtensions
{
    #region Functions
    /// <summary>
    /// Concatenates one array with another.
    /// </summary>
    /// <typeparam name="T">The type of arrays we'd like to join.</typeparam>
    /// <param name="data">The first array to join (head).</param>
    /// <param name="data2">The second array to join (tail)</param>
    /// <returns>Returns a joined array of both the items.</returns>
    public static T[] Concat<T>(this T[] data, T[] data2)
    {
        T[] result = new T[data.Length + data2.Length];
        Array.Copy(data, result, data.Length);
        Array.Copy(data2, 0, result, data.Length, data2.Length);
        return result;
    }

    /// <summary>
    /// Concatenates one array with another.
    /// </summary>
    /// <typeparam name="T">The type of arrays we'd like to join.</typeparam>
    /// <param name="data">The first array to join (head).</param>
    /// <param name="data2">The second array to join (middle)</param>
    /// <param name="data3">The last array to join (tail)</param>
    /// <returns>Returns a joined array of all the items.</returns>
    public static T[] Concat<T>(this T[] data, T[] data2, T[] data3)
    {
        T[] result = new T[data.Length + data2.Length + data3.Length];
        Array.Copy(data, result, data.Length);
        Array.Copy(data2, 0, result, data.Length, data2.Length);
        Array.Copy(data3, 0, result, data.Length + data2.Length, data3.Length);
        return result;
    }

    /// <summary>
    /// Concatenates one array with all the arrays in an array of arrays.
    /// </summary>
    /// <typeparam name="T">The type of arrays we'd like to join.</typeparam>
    /// <param name="data">The first array which contains all the leading items.</param>
    /// <param name="data2">The second array of arrays, for which we obtain all items of the subarrays, and append them to the end of the first array.</param>
    /// <returns>Returns a joined array of all the items.</returns>
    public static T[] Concat<T>(this T[] data, T[][] data2)
    {
        // Get the length of our flat list
        int length = data.Length;
        for (int i = 0; i < data2.Length; i++)
        {
            length += data2[i].Length;
        }

        // Create a byte array for all of our items.
        T[] flatResult = new T[length];

        // Copy all of the data.
        Array.Copy(data, flatResult, data.Length);
        length = data.Length;
        for (int i = 0; i < data2.Length; i++)
        {
            Array.Copy(data2[i], 0, flatResult, length, data2[i].Length);
            length += data2[i].Length;
        }

        // Return our list.
        return flatResult;
    }

    /// <summary>
    /// Sets all data in the array to zero.
    /// </summary>
    /// <param name="data">The array to take a slice out of</param>
    public static void Clear<T>(this T[] data)
    {
        // Clear all bytes as zero
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (T)Convert.ChangeType(0, typeof(T), CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Takes all items from a 3D array and puts them into a 2D array.
    /// </summary>
    /// <typeparam name="T">The type of the array objects</typeparam>
    /// <param name="data">The 3D array to convert into a 2D array.</param>
    /// <returns>Returns a 2D array "flattened" copy of the provided 3D array.</returns>
    public static T[] Flatten<T>(this T[][] data)
    {
        // Get the length of our flat list
        int length = 0;
        for (int i = 0; i < data.Length; i++)
        {
            length += data[i].Length;
        }

        // Create a byte array for all of our items.
        T[] flatResult = new T[length];

        // Copy all of the data.
        length = 0;
        for (int i = 0; i < data.Length; i++)
        {
            Array.Copy(data[i], 0, flatResult, length, data[i].Length);
            length += data[i].Length;
        }

        // Return our list.
        return flatResult;
    }

    /// <summary>
    /// Returns a portion of an array from the given start index (inclusive) to the end.
    /// </summary>
    /// <typeparam name="T">The array element type</typeparam>
    /// <param name="data">The array to take a slice out of</param>
    /// <param name="start">The starting index (can be negative to count from the back).</param>
    /// <returns>Returns the array slice</returns>
    public static T[] Slice<T>(this T[] data, int start)
    {
        return data.Slice(start, data.Length);
    }

    /// <summary>
    /// Returns a portion of an array from the given start index (inclusive) and end index (exclusive)
    /// </summary>
    /// <typeparam name="T">The array element type</typeparam>
    /// <param name="data">The array to take a slice out of</param>
    /// <param name="start">The starting index (can be negative to count from the back).</param>
    /// <param name="end">The ending index (can be negative to count from the back)</param>
    /// <returns>Returns the array slice</returns>
    public static T[] Slice<T>(this T[] data, int start, int end)
    {
        // Handle negative indexing like python
        if (start < 0)
        {
            start = data.Length + start;
        }

        if (end < 0)
        {
            end = data.Length + end;
        }

        end = Math.Min(data.Length, end);

        // Create a new array for our slice
        T[] result = new T[end - start];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = data[i + start];
        }

        return result;
    }

    /// <summary>
    /// Verifies that the underlying values in an array all equal, and there is the same amount of items in both arrays.
    /// </summary>
    /// <typeparam name="T">The type of underlying array object to check.</typeparam>
    /// <param name="data">The first array to compare.</param>
    /// <param name="data2">The second array to compare.</param>
    /// <returns>Returns a boolean indicating if all underlying items in the arrays equal.</returns>
    public static bool ValuesEqual<T>(this T[] data, T[] data2)
    {
        // Verify our lengths match.
        if (data.Length != data2.Length)
        {
            return false;
        }

        // If our items are the same, return true
        if (data == data2)
        {
            return false;
        }

        // Compare all items
        for (int i = 0; i < data.Length; i++)
        {
            if (!data[i].Equals(data2[i]))
            {
                return false;
            }
        }

        // Otherwise we passed the test
        return true;
    }
    #endregion
}