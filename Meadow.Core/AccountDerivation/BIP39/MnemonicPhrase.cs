using Meadow.Core.Utils;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Meadow.Core.AccountDerivation.BIP39
{
    /// <summary>
    /// Represents mnemonic phrases which can be derived into an encryption key, with a given password,
    /// as defined by BIP39.
    /// </summary>
    public class MnemonicPhrase
    {
        #region Constants
        /// <summary>
        /// Constant used in salt computation for the 
        /// </summary>
        private const string SALT_COMPUTATION_PREFIX = "mnemonic";
        /// <summary>
        /// The amount of bytes in our entropy data per checksum bit.
        /// </summary>
        private const int MNEMONIC_ENTROPY_BITS_PER_CHECKSUM_BIT = 32;
        /// <summary>
        /// The amount of words encompassed in a single checksum bit.
        /// </summary>
        private const int MNEMONIC_WORDS_PER_CHECKSUM_BIT = 3;
        /// <summary>
        /// (Inclusive) Minimum amount of words in our mnemonic phrase.
        /// </summary>
        private const int MNEMONIC_WORDS_MINIMUM = 12;
        /// <summary>
        /// (Inclusive) Maximum amount of words in our mnemonic phrase.
        /// </summary>
        private const int MNEMONIC_WORDS_MAXIMUM = 24;
        /// <summary>
        /// The amount of bits used to represent a word index.
        /// </summary>
        private const int MNEMONIC_WORD_INDEX_BITCOUNT = 11;
        #endregion

        #region Fields
        private static UTF8Encoding _encoder = new UTF8Encoding(false);
        private static RandomNumberGenerator _random = RandomNumberGenerator.Create();
        private static SHA256 sha256Provider = SHA256.Create();
        #endregion

        #region Properties
        public string MnemonicString { get; }
        public string[] MnemonicWords { get; }
        public WordList WordList { get; }
        public int[] Indices { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a mnemonic phrase from the given mnemonic string.
        /// </summary>
        /// <param name="mnemonic">The mnemonic string to initialize this mnemonic phrase from, for key derivation.</param>
        public MnemonicPhrase(string mnemonic)
        {
            // If our argument is null, we throw an exception, otherwise we set our mnemonic string.
            MnemonicString = mnemonic.Trim() ?? throw new ArgumentException("Provided mnemonic string was null or empty.");

            // Determine the word list (language) the mnemonic string belongs to by checking indicies.
            WordList = WordList.GetWordList(MnemonicString);

            // Obtain our words from our mnemonic.
            MnemonicWords = WordList.SplitMnemonic(MnemonicString);

            // Verify our word count is valid
            if (MnemonicWords.Length < MNEMONIC_WORDS_MINIMUM || MnemonicWords.Length > MNEMONIC_WORDS_MAXIMUM)
            {
                throw new ArgumentException($"Provided entropy data of this size would represent {MnemonicWords.Length} words. Should be between {MNEMONIC_WORDS_MINIMUM}-{MNEMONIC_WORDS_MAXIMUM} (inclusive).");
            }

            // Initialize our indices array.
            Indices = new int[MnemonicWords.Length];

            // Loop for each index we want to resolve for the words in our mnemonics
            for (int i = 0; i < Indices.Length; i++)
            {
                // Determine the word index for the indexed word.
                int currentWordIndex = WordList.GetWordIndex(MnemonicWords[i]);

                // If the index is invalid, throw an exception.
                if (currentWordIndex < 0)
                {
                    throw new ArgumentException($"Could not resolve word \"{MnemonicWords[i]}\" in word list for language \"{WordList.Language}\".");
                }

                // Set the word index.
                Indices[i] = currentWordIndex;
            }
        }

        /// <summary>
        /// Initializes a mnemonic phrase for the given language, with the optionally given entropy data (or newly generated data if none is provided).
        /// </summary>
        /// <param name="wordListLanguage">The language to create a mnemonic phrase in.</param>
        /// <param name="entropy">The optionally given entropy data to generate a mnemonic phrase from. If null, generates new entropy data of the maximum size.</param>
        public MnemonicPhrase(WordListLanguage wordListLanguage, byte[] entropy = null)
        {
            // Set our word list
            WordList = WordList.GetWordList(wordListLanguage);

            // If our entropy is null, initialize a new set.
            if (entropy == null)
            {
                // Create a buffer of random bytes (entropy) using our RNG.
                int maximumEntropySize = (MNEMONIC_ENTROPY_BITS_PER_CHECKSUM_BIT * (MNEMONIC_WORDS_MAXIMUM / MNEMONIC_WORDS_PER_CHECKSUM_BIT)) / 8;
                entropy = new byte[maximumEntropySize];
                _random.GetBytes(entropy);
            }

            // Verify our entropy size is divisible by the range of a checksum.
            int entropySizeBits = (entropy.Length * 8);
            if (entropySizeBits % MNEMONIC_ENTROPY_BITS_PER_CHECKSUM_BIT != 0)
            {
                throw new ArgumentException($"Provided entropy data must be divisible by {MNEMONIC_ENTROPY_BITS_PER_CHECKSUM_BIT}");
            }

            // Calculate our component sizes.
            int checksumBitcount = entropySizeBits / MNEMONIC_ENTROPY_BITS_PER_CHECKSUM_BIT;
            int wordCount = checksumBitcount * MNEMONIC_WORDS_PER_CHECKSUM_BIT;

            // Verify our word count is valid
            if (wordCount < MNEMONIC_WORDS_MINIMUM || wordCount > MNEMONIC_WORDS_MAXIMUM)
            {
                throw new ArgumentException($"Provided entropy data of this size would represent {wordCount} words. Should be between {MNEMONIC_WORDS_MINIMUM}-{MNEMONIC_WORDS_MAXIMUM} (inclusive).");
            }

            // Calculate our SHA256 hash of the entropy.
            byte[] checksum = sha256Provider.ComputeHash(entropy);

            // Now we write the entropy followed by the checksum bits (which we will use to interpret a mnemonic from later).
            BitStream bitStream = new BitStream();
            bitStream.WriteBytes(entropy);
            bitStream.WriteBytes(checksum, checksumBitcount);

            // Move back to the start of the data
            bitStream.Position = 0;
            bitStream.BitPosition = 0;

            // Read every word index, which are represented by 11-bit unsigned integers. At the same time, we resolve our mnemonic words.
            int wordIndexCount = (int)(bitStream.BitLength / MNEMONIC_WORD_INDEX_BITCOUNT);
            Indices = new int[wordIndexCount];
            MnemonicWords = new string[Indices.Length];
            for (int i = 0; i < Indices.Length; i++)
            {
                Indices[i] = (int)bitStream.ReadUInt64(MNEMONIC_WORD_INDEX_BITCOUNT);
                MnemonicWords[i] = WordList.Words[Indices[i]];
            }

            // Close the bitstream.
            bitStream.Close();

            // Join all strings into a mnemonic sentence.
            MnemonicString = WordList.JoinMnemonic(MnemonicWords);
        }
        #endregion

        #region Functions
        /// <summary>
        /// Verifies the mnemonic phrase's word sequence checksum is valid.
        /// </summary>
        /// <returns>Returns true if the mnemonic word sequence is valid.</returns>
        public bool Verify()
        {
            // Verify our index count/word count is in our bounds.
            if (Indices.Length < MNEMONIC_WORDS_MINIMUM && Indices.Length > MNEMONIC_WORDS_MAXIMUM)
            {
                return false;
            }

            // Determine the size of our entropy and checksum in bits.
            int bitStreamLength = Indices.Length * MNEMONIC_WORD_INDEX_BITCOUNT;
            int checksumBitcount = bitStreamLength / (MNEMONIC_ENTROPY_BITS_PER_CHECKSUM_BIT + 1);
            int entropyBitcount = checksumBitcount * MNEMONIC_ENTROPY_BITS_PER_CHECKSUM_BIT;

            // Now we'll want to write the indices back to a bitstream to parse our entropy/checksum from it.
            BitStream bitStream = new BitStream();
            for (int i = 0; i < Indices.Length; i++)
            {
                // If our index is negative, return false
                if (Indices[i] < 0)
                {
                    return false;
                }

                // Write the word index.
                bitStream.Write((ulong)Indices[i], MNEMONIC_WORD_INDEX_BITCOUNT);
            }

            // Move back to the start of the data
            bitStream.Position = 0;
            bitStream.BitPosition = 0;

            // Read our entropy data.
            byte[] entropy = bitStream.ReadBytes(entropyBitcount, true);

            // Read our checksum
            byte[] checksum = bitStream.ReadBytes(checksumBitcount, true);

            // Recalculate our hash of the entropy and derive our checksum from its bits sequentially.
            byte[] calculatedChecksum = sha256Provider.ComputeHash(entropy);
            bitStream.ClearContents();
            bitStream.WriteBytes(calculatedChecksum, checksumBitcount);

            // Move back to the start of the data
            bitStream.Position = 0;
            bitStream.BitPosition = 0;

            // Read our calculated checksum.
            calculatedChecksum = bitStream.ReadBytes(checksumBitcount, true);

            // Close the bitstream.
            bitStream.Close();

            // Compare our sequences.
            return checksum.SequenceEqual(calculatedChecksum);
        }

        /// <summary>
        /// Calculates/derives the seed for a key from this mnemonic.
        /// </summary>
        /// <param name="password">The password to use when calculating our key seed.</param>
        /// <returns>Returns the calculated key seed from this mnemonic.</returns>
        public byte[] DeriveKeySeed(string password = null)
        {
            // If the string is null, we instead use a blank string for our concatenation
            if (password == null)
            {
                password = "";
            }

            // Obtain our salt from the passphrase.
            byte[] normalizedPassBytes = NormalizeStringToBytes(password);
            byte[] salt = _encoder.GetBytes(SALT_COMPUTATION_PREFIX).Concat(normalizedPassBytes).ToArray();

            // Obtain our mnemonic string data.
            string data = NormalizeString(MnemonicString);

            // Derive our key
            return KeyDerivation.Pbkdf2(data, salt, KeyDerivationPrf.HMACSHA512, 2048, 64);
        }

        /// <summary>
        /// Normalize the given string for different cultures/localizations.
        /// </summary>
        /// <param name="s">The string to normalize.</param>
        /// <returns>Returns the given string, normalized.</returns>
        public static string NormalizeString(string s)
        {
            // Normalize our string
            string normalizedS = s.Normalize(NormalizationForm.FormKD);

            // Return our normalized string
            return normalizedS;
        }

        /// <summary>
        /// Normalize the given string for different cultures/localizations, and convert it to bytes using our encoder.
        /// </summary>
        /// <param name="s">The string to normalize and convert into bytes.</param>
        /// <returns>Returns the given string, normalized, as bytes.</returns>
        public static byte[] NormalizeStringToBytes(string s)
        {
            // Normalize our string
            string normalizedS = NormalizeString(s);

            // Return the string as bytes.
            return _encoder.GetBytes(normalizedS);
        }
        #endregion
    }
}
