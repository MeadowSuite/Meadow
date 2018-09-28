using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Meadow.Core.AccountDerivation.BIP32
{
    /// <summary>
    /// Represents a path in derived hierarchically deterministic keys, as defined by BIP32.
    /// </summary>
    public class KeyPath
    {
        #region Constants
        private const uint HARDENED_MASK = (uint)1 << 31;
        private const string PATH_ROOT = "m";
        private const char PATH_SEPERATOR = '/';
        private const char HARDENED_DIRECTORY_SYMBOL = '\'';
        #endregion

        #region Fields
        private string _cachedStringPath;
        #endregion

        #region Properties
        /// <summary>
        /// Determines our indices representing this path.
        /// </summary>
        public uint[] Indices { get; }

        /// <summary>
        /// The parent key path to this key path. (One directory above, or null if this is a top level key path).
        /// </summary>
        public KeyPath Parent
        {
            get
            {
                // The parent of this current path is just one directory above.
                if (Indices.Length > 0)
                {
                    // This path has a parent, it's path is one directory above.
                    return new KeyPath(Indices.Take(Indices.Length - 1).ToArray());
                }
                else
                {
                    // This path is top level, it has no parent.
                    return null;
                }
            }
        }

        /// <summary>
        /// Represents the current path leading to a hardened derived key.
        /// </summary>
        public bool Hardened
        {
            get
            {
                // Verify we have indices
                if (Indices.Length > 0)
                {
                    // Check if our last path directory is hardened (meaning this path leads to a hardened key).
                    return CheckHardenedDirectoryIndex(Indices[Indices.Length - 1]);
                }
                else
                {
                    // If we have no items, this is obviously not a hardened path.
                    return false;
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the key path with a given string representation of a key path.
        /// </summary>
        /// <param name="path">The string representation of the key path to initialize with.</param>
        public KeyPath(string path)
        {
            // Split the path by our seperator character, and remove the root node (constant, unnecessary, we only want index paths) to
            // obtain all the directories in this path which represent indices.
            Indices = path.Split(new char[] { PATH_SEPERATOR }, StringSplitOptions.RemoveEmptyEntries)
                .Where(d => d != PATH_ROOT)
                .Select(d => DirectoryToIndex(d))
                .ToArray();
        }

        /// <summary>
        /// Initializes the key path with a given integer index path representation of a key path.
        /// </summary>
        /// <param name="indices">The integer index path representation of a key path to initialize with.</param>
        public KeyPath(uint[] indices)
        {
            // Set our indexes.
            Indices = indices;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Converts a single directory from the path into an index.
        /// </summary>
        /// <param name="directory">A single entry from the path split at each seperator/delimiter <see cref="HARDENED_DIRECTORY_SYMBOL"/>.</param>
        /// <returns>Returns an index for the key path with the appropriate hardened flag set.</returns>
        private uint DirectoryToIndex(string directory)
        {
            // There are 2^31 non-hardened, and 2^31 hardened indices. They are represented in a 32-bit integer,
            // with the most significant bit signifying the directory as hardened if the bit is set.
            // In string representation, hardened strings end with a ' (prime symbol).
            if (directory.Length == 0)
            {
                throw new ArgumentException("Provided key path failed to parse due to a blank directory in the path.");
            }

            // If there is a prime symbol at the end, this is a hardened string.
            bool hardenedDirectory = directory[directory.Length - 1] == HARDENED_DIRECTORY_SYMBOL;
            string baseDirectory = directory;

            // If it is hardened, remove the prime symbol.
            if (hardenedDirectory)
            {
                baseDirectory = baseDirectory.Substring(0, baseDirectory.Length - 1);
            }

            // Try to parse the index from the path.
            bool success = uint.TryParse(baseDirectory, out uint index);
            if (!success)
            {
                throw new ArgumentException("Provided key path could not be parsed because a base directory could not be converted to an integer index.");
            }

            // If our directory is hardened, we set the highest bit.
            if (hardenedDirectory)
            {
                index |= HARDENED_MASK;
            }

            // Return the index
            return index;
        }

        /// <summary>
        /// Converts a single integer index into the appropriate directory/single entry string to be joined in the path.
        /// </summary>
        /// <param name="index">The index to convert to a directory/entry string to be joined in the path.</param>
        /// <returns>Returns the directory/entry string which represents the provided index, to be joined into the full path.</returns>
        private string IndexToDirectory(uint index)
        {
            // Determine if our index is hardened.
            bool hardenedDirectory = CheckHardenedDirectoryIndex(index);

            // Obtain our base index
            uint baseIndex = index & ~HARDENED_MASK;

            // Create our resulting directory.
            string directory = index.ToString(CultureInfo.InvariantCulture);

            // If this is a hardened directory, we add the prime symbol.
            if (hardenedDirectory)
            {
                directory += HARDENED_DIRECTORY_SYMBOL;
            }

            // Return the directory
            return directory;
        }

        /// <summary>
        /// Concatenates the string representation of a path to the current key path and returns the result.
        /// </summary>
        /// <param name="pathToAppend">The path to concatenate to the end of the current key path.</param>
        /// <returns>Returns the key path resulting from this key path concatenated with the provided path.</returns>
        public KeyPath Concat(string pathToAppend)
        {
            // Return our result obtained by concatenating indices.
            return Concat(pathToAppend);
        }

        /// <summary>
        /// Concatenates the given key path to the current key path and returns the result.
        /// </summary>
        /// <param name="pathToAppend">The path to concatenate to the end of the current key path.</param>
        /// <returns>Returns the key path resulting from this key path concatenated with the provided path.</returns>
        public KeyPath Concat(KeyPath pathToAppend)
        {
            // Return our result obtained by concatenating indices.
            return Concat(pathToAppend.Indices);
        }


        /// <summary>
        /// Concatenates the indices representing a key path to the current key path and returns the result.
        /// </summary>
        /// <param name="indices">The indices representing a key path to concenate to the current path.</param>
        /// <returns>Returns the key path resulting from this key path concatenated with the provided indices representing a key path.</returns>
        public KeyPath Concat(uint[] indices)
        {
            // We'll want to concatenate our indices
            uint[] concatenatedIndices = Indices.Concat(indices).ToArray();

            // Return a new key path
            return new KeyPath(concatenatedIndices);
        }

        /// <summary>
        /// Obtains a key path that exists at the same level, but has it's final index in the path incremented.
        /// </summary>
        /// <returns>Returns a key path that exists at the same level, but has it's final index in the path incremented.</returns>
        public KeyPath Next()
        {
            // Verify we have indices
            if (Indices.Length == 0)
            {
                throw new ArgumentException("Failed to increment key path because the current key path has no indices.");
            }

            // We'll want to copy our indices, and increment the last index.
            uint[] clonedIndices = (uint[])Indices.Clone();

            // Increment our final index
            clonedIndices[clonedIndices.Length]++;

            // Return a new key path with our copied indices
            return new KeyPath(clonedIndices);
        }

        /// <summary>
        /// Obtains a key path that exists at the same level, but has it's final index in the path decremented.
        /// </summary>
        /// <returns>Returns a key path that exists at the same level, but has it's final index in the path decremented.</returns>
        public KeyPath Previous()
        {
            // Verify we have indices
            if (Indices.Length == 0)
            {
                throw new ArgumentException("Failed to decrement key path because the current key path has no indices.");
            }

            // We'll want to copy our indices, and decrement the last index.
            uint[] clonedIndices = (uint[])Indices.Clone();

            // Verify our index isn't 0 (without hardened bit)
            uint baseIndex = (clonedIndices[clonedIndices.Length - 1] & ~HARDENED_MASK);
            if (baseIndex == 0)
            {
                throw new ArgumentException("Failed to decrement key path because the current key path's last index is zero, and decrementing it would underflow and invert the hardened flag.");
            }

            // Increment our final index
            clonedIndices[clonedIndices.Length - 1]--;

            // Return a new key path with our copied indices
            return new KeyPath(clonedIndices);
        }

        /// <summary>
        /// Indicates if a directory index is hardened or not.
        /// </summary>
        /// <param name="index">The index to check hardened status of.</param>
        /// <returns>Returns true if the index has the hardened bit set.</returns>
        public static bool CheckHardenedDirectoryIndex(uint index)
        {
            // Determine if our hardened bit is set.
            return (index & HARDENED_MASK) != 0;
        }

        public override bool Equals(object obj)
        {
            // Verify the type of our object
            if (obj == null || !(obj is KeyPath))
            {
                return false;
            }

            // Obtain the object as a key path
            KeyPath otherPath = (KeyPath)obj;

            // Verify we have the same number of indices
            if (Indices.Length != otherPath.Indices.Length)
            {
                return false;
            }

            // Verify the underlying indices match, if so, the path is the same.
            return Indices.SequenceEqual(otherPath.Indices);
        }

        public override int GetHashCode()
        {
            // Obtain our hash code of our string path, such that different instances pointing to the same path
            // are treated equally.
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            // If we do not have a cached string path, we construct it.
            if (_cachedStringPath == null)
            {
                // We do not return with our path root we might have constructed with, because
                // we may have not initialized with the path root or may not want to include it.

                // We'll want our string representation to represent the underlying path string.
                string[] directories = Indices.Select(i => IndexToDirectory(i)).ToArray();

                // Join our directories with our path seperator
                _cachedStringPath = string.Join(PATH_SEPERATOR.ToString(CultureInfo.InvariantCulture), directories);
            }

            // Return our cached path.
            return _cachedStringPath;
        }

        public static bool operator ==(KeyPath first, KeyPath second)
        {
            // If one item is null but not the other, it doesn't match.
            if ((first is null) != (second is null))
            {
                return false;
            }
            else if ((first is null) && (second == null))
            {
                // If both items are null, they are equal.
                return true;
            }

            // Return true if the object is the same, or the first equals the second (using our override).
            return ReferenceEquals(first, second) || first.Equals(second);
        }

        public static bool operator !=(KeyPath first, KeyPath second)
        {
            // Return the opposite of the equals comparison.
            return !(first == second);
        }

        #endregion
    }
}
