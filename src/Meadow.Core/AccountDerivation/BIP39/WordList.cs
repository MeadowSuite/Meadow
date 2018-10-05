using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Meadow.Core.AccountDerivation.BIP39
{
    /// <summary>
    /// Represents the list of words used to generate/parse mnemonic phrases. (Unique per language).
    /// </summary>
    public class WordList
    {
        #region Constants
        /// <summary>
        /// The seperator used generically across most languages/localizations.
        /// </summary>
        private const char WORD_SEPARATOR_GENERIC = ' ';
        /// <summary>
        /// The seperator used by the Japanese language/localization.
        /// </summary>
        private const char WORD_SEPERATOR_JAPANESE = '\u3000';
        #endregion

        #region Fields
        private static Dictionary<WordListLanguage, WordList> _cachedWordLists;
        #endregion

        #region Properties
        /// <summary>
        /// Describes the language that this word list describes.
        /// </summary>
        public WordListLanguage Language { get; }
        /// <summary>
        /// A string array representing all the words in the word list.
        /// </summary>
        public string[] Words { get; }
        /// <summary>
        /// The seperator/spacing character between words in a mnemonic.
        /// </summary>
        public char Seperator
        {
            get
            {
                // If our language is japanese, our space/seperator character is a different character
                if (Language == WordListLanguage.Japanese)
                {
                    return WORD_SEPERATOR_JAPANESE;
                }

                // Otherwise any other language has a typical space character.
                return WORD_SEPARATOR_GENERIC;
            }
        }
        #endregion

        #region Constructor
        static WordList()
        {
            // Initialize our word list cache.
            _cachedWordLists = new Dictionary<WordListLanguage, WordList>(); 
        }

        /// <summary>
        /// Base constructor to initialize a word list to represent the given language.
        /// </summary>
        /// <param name="language">The language which this word list will represent.</param>
        private WordList(WordListLanguage language)
        {
            // Set our language
            Language = language;
        }

        /// <summary>
        /// Initializes a word list to represent the given language, with the given words.
        /// </summary>
        /// <param name="language">The language which this word list will represent.</param>
        /// <param name="words">The words which constitute the word list to initialize.</param>
        private WordList(WordListLanguage language, string[] words) : this(language)
        {
            // Set our words (normalized).
            Words = words.Select(s => MnemonicPhrase.NormalizeString(s)).ToArray();
        }

        /// <summary>
        /// Initializes a word list to represent the given language, with the given words.
        /// </summary>
        /// <param name="language">The language which this word list will represent.</param>
        /// <param name="words">The words which constitute the word list to initialize.</param>
        private WordList(WordListLanguage language, string words) : this(language)
        {
            // Split our words out
            Words = words.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
        #endregion

        #region Functions
        /// <summary>
        /// Obtains the word list for a given mnemonic string by auto detecting the language used.
        /// </summary>
        /// <param name="mnemonicString">The mnemonic string to parse to determine the language of the word list to obtain.</param>
        /// <returns>Returns the word list in the appropriate language which corresponds to the given mnemonic string.</returns>
        public static WordList GetWordList(string mnemonicString)
        {
            // Split the mnemonic string.
            string[] words = SplitMnemonic(mnemonicString);

            // Get all languages.
            WordListLanguage[] languages = (WordListLanguage[])Enum.GetValues(typeof(WordListLanguage));

            // Loop for each word
            foreach (string word in words)
            {
                // Loop for each language.
                foreach (WordListLanguage language in languages)
                {
                    // Obtain the word list for this language
                    WordList wordList = GetWordList(language);

                    // Check if the word list contains the word, return it.
                    if (wordList.GetWordIndex(word) >= 0)
                    {
                        return wordList;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Obtains the word list for a given language for mnemonic phrases, or if it does not exist,
        /// parses one from the embedded word list resources in this assembly.
        /// </summary>
        /// <param name="language">The language of the word list to obtain.</param>
        /// <returns>Returns a word list for the given language, used for mnemonic phrases.</returns>
        public static WordList GetWordList(WordListLanguage language)
        {
            lock (_cachedWordLists)
            {
                // Try to get our word list
                bool success = _cachedWordLists.TryGetValue(language, out WordList result);
                if (success)
                {
                    return result;
                }

                // Obtain the appropriate word list from our resource files.
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                string resourceName = null;
                switch (language)
                {
                    case WordListLanguage.Chinese_Simplified:
                        resourceName = "wordlist_chinese_simplified.txt";
                        break;
                    case WordListLanguage.Chinese_Traditional:
                        resourceName = "wordlist_chinese_traditional.txt";
                        break;
                    case WordListLanguage.English:
                        resourceName = "wordlist_english.txt";
                        break;
                    case WordListLanguage.French:
                        resourceName = "wordlist_french.txt";
                        break;
                    case WordListLanguage.Italian:
                        resourceName = "wordlist_italian.txt";
                        break;
                    case WordListLanguage.Japanese:
                        resourceName = "wordlist_japanese.txt";
                        break;
                    case WordListLanguage.Korean:
                        resourceName = "wordlist_korean.txt";
                        break;
                    case WordListLanguage.Spanish:
                        resourceName = "wordlist_spanish.txt";
                        break;
                }

                // If our resource name is null, throw an exception
                if (resourceName == null)
                {
                    throw new ArgumentException($"Could not obtain an embedded word list for language \"{resourceName}\".");
                }

                // Obtain our full resource name (it should end with the resource filename).
                resourceName = currentAssembly.GetManifestResourceNames().First(x =>
                {
                    int targetIndex = x.Length - resourceName.Length;
                    if (targetIndex < 0)
                    {
                        return false;
                    }

                    return x.LastIndexOf(resourceName, StringComparison.OrdinalIgnoreCase) == targetIndex;
                });

                // Open a stream on the resource
                Stream stream = currentAssembly.GetManifestResourceStream(resourceName);

                // Read all the text from the stream.
                StreamReader streamReader = new StreamReader(stream);
                string allText = streamReader.ReadToEnd();
                streamReader.Close();
                stream.Close();

                // Create a word list instance
                WordList wordList = new WordList(language, allText);

                // Set it in our cached word lists
                _cachedWordLists[language] = wordList;

                // Return our word list
                return wordList;
            }
        }

        /// <summary>
        /// Obtains the word list for a given language for mnemonic phrases, or if it does not exist,
        /// creates one from the given words.
        /// </summary>
        /// <param name="language">The language of the word list to obtain.</param>
        /// <param name="words">The array of words to use to create the word list if one was not already parsed.</param>
        /// <returns>Returns a word list for the given language, used for mnemonic phrases.</returns>
        public static WordList GetWordList(WordListLanguage language, string[] words)
        {
            lock (_cachedWordLists)
            {
                // Try to get our word list
                bool success = _cachedWordLists.TryGetValue(language, out WordList result);
                if (success)
                {
                    return result;
                }

                // If we couldn't get it, parse a new one
                WordList wordList = new WordList(language, words);

                // Set it in our lookup
                _cachedWordLists[language] = wordList;

                // Return our word list.
                return wordList;
            }
        }

        /// <summary>
        /// Obtains the word list for a given language for mnemonic phrases, or if it does not exist,
        /// reads one from the given filename.
        /// </summary>
        /// <param name="language">The language of the word list to obtain.</param>
        /// <param name="fileName">The filename of the word list to read from if one was not already parsed.</param>
        /// <returns>Returns a word list for the given language, used for mnemonic phrases.</returns>
        public static WordList GetWordList(WordListLanguage language, string fileName)
        {
            lock (_cachedWordLists)
            {
                // Try to get our word list
                bool success = _cachedWordLists.TryGetValue(language, out WordList result);
                if (success)
                {
                    return result;
                }

                // If we couldn't get it, parse a new one
                string allText = File.ReadAllText(fileName);
                WordList wordList = new WordList(language, allText);

                // Set it in our lookup
                _cachedWordLists[language] = wordList;

                // Return our word list.
                return wordList;
            }
        }

        /// <summary>
        /// Determines the index of the given word in the current word list.
        /// </summary>
        /// <param name="word">The word to get the index of in the current word list.</param>
        /// <returns>Returns the index of the provided word in the current word list. Returns a negative integer if the word could not be found.</returns>
        public int GetWordIndex(string word)
        {
            // Normalize the word
            string normalizedWord = MnemonicPhrase.NormalizeString(word);

            // Determine the word index.
            int wordIndex = Array.FindIndex(Words, w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase));

            // Return the word index
            return wordIndex;
        }

        /// <summary>
        /// Joins mnemonic word components into a single mnemonic phrase.
        /// </summary>
        /// <param name="words">An array of mnemonic words to join.</param>
        /// <returns>Returns a string of all joined words.</returns>
        public string JoinMnemonic(string[] words)
        {
            // Define our result string
            string result = "";

            // Loop for every word to join.
            for (int i = 0; i < words.Length; i++)
            {
                // Add our word to our result string
                result += words[i];

                // If it's not our last word, we add a seperator.
                if (i < words.Length - 1)
                {
                    result += Seperator;
                }
            }

            // Return our result
            return result;
        }

        /// <summary>
        /// Splits a mnemonic string into its word components.
        /// </summary>
        /// <param name="mnemonicString">The mnemonic string to split into words.</param>
        /// <returns>Returns a string array that represents the word components of the mnemonic string.</returns>
        public static string[] SplitMnemonic(string mnemonicString)
        {
            // Split the mnemonic with the appropriate space variable.
            string[] result = mnemonicString.Split(new char[] { WORD_SEPARATOR_GENERIC, WORD_SEPERATOR_JAPANESE }, StringSplitOptions.RemoveEmptyEntries);

            // Return our result
            return result;
        }
        #endregion
    }
}
