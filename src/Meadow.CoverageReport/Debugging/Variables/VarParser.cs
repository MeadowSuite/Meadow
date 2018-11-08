using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.AstTypes.Enums;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Meadow.CoverageReport.Debugging.Variables
{
    public abstract class VarParser
    {
        #region Constants
        /// <summary>
        /// The default bit count for int/uint solidity types.
        /// </summary>
        private static string _cachedLocationRegexGroup = null;
        static object _cacheSyncRoot = new object();

        private static Dictionary<string, VarTypeLocation> _locationLookup = new Dictionary<string, VarTypeLocation>()
        {
            { "storage ref", VarTypeLocation.StorageRef },
            { "storage pointer", VarTypeLocation.StoragePtr },
            { "memory", VarTypeLocation.Memory },
            { "calldata", VarTypeLocation.CallData }
        };
        #endregion

        #region Functions
        /// <summary>
        /// Obtains the size of a solidity int/uint type in bytes when provided a full type/location string.
        /// </summary>
        /// <param name="baseType">The variable base type for which we wish to obtain the integer byte size of.</param>
        /// <param name="genericType">Optional generic type obtained from the base type. If null, it is reparsed.</param>
        /// <returns>Returns the byte count for the provided integer type.</returns>
        public static int GetIntegerSizeInBytes(string baseType, VarGenericType? genericType = null)
        {
            // If our generic type isn't set, obtain it from our base type.
            if (!genericType.HasValue)
            {
                genericType = GetGenericType(baseType);
            }

            // Verify our generic type
            if (genericType != VarGenericType.Int && genericType != VarGenericType.UInt)
            {
                throw new ArgumentException("Could not obtain integer byte size because a non-integer type string was supplied.");
            }

            // Try to parse an integer suffix from our type.
            Match match = Regex.Match(baseType, @"(int|uint)(\d+)");
            if (match.Success)
            {
                return int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) / 8;
            }
            else
            {
                // If we failed to parse, the default integer size for uint/int is 256 bit.
                return 256 / 8;
            }
        }

        /// <summary>
        /// Obtains the size of a solidity fixed array size type in bytes when provided a full type/location string.
        /// </summary>
        /// <param name="baseType">The variable base type for which we wish to obtain the integer byte size of.</param>
        /// <returns>Returns the byte count for the provided fixed array size.</returns>
        public static int GetFixedArraySizeInBytes(string baseType)
        {
            // Check if this is a bytes1-bytes32
            Match match = Regex.Match(baseType, @"bytes(3[0-2]|[12][0-9]|[1-9])");
            if (match.Success)
            {
                // Parse the size and return it.
                return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            // Return our size we parsed.
            throw new ArgumentException("Could not obtain fixed array byte size from type.");
        }

        public static (string elementType, int? arraySize) ParseArrayTypeComponents(string baseType)
        {
            // Parse our array base type.

            // Try to parse an our array components.
            Match match = Regex.Match(baseType, @"^(.*)\[\s*(\d*)\s*\]$");
            if (!match.Success)
            {
                throw new ArgumentException("Could not obtain array type components from array type.");
            }

            // Define our results
            string elementType = match.Groups[1].Value;
            string arraySizeStr = match.Groups[2].Value;
            bool sizeParsed = int.TryParse(arraySizeStr, out int arraySize);

            // Return our results.
            return (elementType, sizeParsed ? (int?)arraySize : null);
        }

        /// <summary>
        /// Parses the base type and optional location from a type string. Some versions of solidity include location in type string, 
        /// so this method is intended to seperate the underlying type from location.
        /// </summary>
        /// <param name="type">The type string to parse components from.</param>
        /// <returns>Returns the underlying type and an optional location parsed from the type string.</returns>
        public static (string baseType, VarTypeLocation location) ParseTypeComponents(string type)
        {
            // Verify our type is valid
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException("Invalid type string provided to solidity variable type parser (null or empty).");
            }

            // We'll want to match our data using regular expressions, with (1) representing type, (2) whitespace, and (3) location as follows:
            // 1) "^(.*\S)" : Captures anything up until the last non-white space character before our location (3).
            // 2) "\s+" : We make sure there is at least one whitespace character separating what should be type and location.
            // 3) "({_cachedLocationRegexGroup}).*" : Finally, we capture by matching possible locations, and let the rest of the string be anything.
            // Note: _cachedLocationRegexGroup refers to a chain of possible location strings, in an "|" (OR) pattern, such that we match to any of them.
            // Example: As of writing this, the final expression will be "^(.*\S)\s+(storage ref|storage pointer|memory|calldata).*"
            lock (_cacheSyncRoot)
            {
                // First we create the type matching group string if we haven't already.
                if (_cachedLocationRegexGroup == null)
                {
                    // Build our location string capture group.
                    _cachedLocationRegexGroup = "";
                    string[] locationKeys = new string[_locationLookup.Count];
                    _locationLookup.Keys.CopyTo(locationKeys, 0);
                    for (int i = 0; i < locationKeys.Length; i++)
                    {
                        _cachedLocationRegexGroup += locationKeys[i];
                        if (i < locationKeys.Length - 1)
                        {
                            _cachedLocationRegexGroup += "|";
                        }
                    }
                }

                // Create our regular expression to capture all information from our type.
                Regex regularExpression = new Regex($@"^(.*\S)\s+({_cachedLocationRegexGroup}).*", RegexOptions.IgnoreCase);

                // Match the regular expression pattern against a text string.
                Match match = regularExpression.Match(type);

                // Verify we matched, if not, then there was no location in the type to extract, instead the whole string is likely just base type.
                if (!match.Success)
                {
                    return (type, VarTypeLocation.NoneSpecified);
                }

                // Finally, we'll have two groups, one for base type, one for location.
                string baseType = match.Groups[1].Value;
                string locationString = match.Groups[2].Value.ToLowerInvariant();

                // Return the 
                return (baseType, _locationLookup[locationString]);

            }
        }

        /// <summary>
        /// Obtains a generic type when provided a full type/location string.
        /// </summary>
        /// <param name="baseType">The full type/location string to derive a generic type from.</param>
        /// <returns>Returns a generic type derived from the provided full type/location.</returns>
        public static VarGenericType GetGenericType(string baseType)
        {
            // If this expression has the "mapping" word in it, we classify it as a mapping.
            Match match = Regex.Match(baseType, ".*mapping.*");
            if (match.Success)
            {
                return VarGenericType.Mapping;
            }

            // If this expression ends with a closing square bracket, we classify it was an array.
            match = Regex.Match(baseType, ".*]$");
            if (match.Success)
            {
                return VarGenericType.Array;
            }

            // Obtain the first word.
            match = Regex.Match(baseType, @"\s*(\S+).*");
            if (!match.Success)
            {
                throw new ArgumentException($"Invalid type \"{baseType ?? "<null>"}\". Could not find a non-whitespace character to match in type.");
            }

            baseType = match.Groups[1].Value.Trim();

            // Check if this is a bytes1-bytes32
            match = Regex.Match(baseType, @"bytes(3[0-2]|[12][0-9]|[1-9])");
            if (match.Success)
            {
                return VarGenericType.ByteArrayFixed;
            }

            // Remove bitcount of int/uint, or any other numbers at the end of the type.
            // We do this by capturing the type before the last digits
            match = Regex.Match(baseType, @"(.*\D)\d+");
            if (match.Success)
            {
                baseType = match.Groups[1].Value;
            }

            // And handle the remainder of our types.
            switch (baseType)
            {
                case "address":
                case "contract":
                    return VarGenericType.Address;
                case "bool":
                    return VarGenericType.Boolean;
                case "bytes":
                    return VarGenericType.ByteArrayDynamic;
                case "enum":
                    return VarGenericType.Enum;
                case "string":
                    return VarGenericType.String;
                case "struct":
                    return VarGenericType.Struct;
                case "int":
                    return VarGenericType.Int;
                case "uint":
                    return VarGenericType.UInt;
            }

            // We were unable to obtain our variable object, throw an exception.
            throw new ArgumentException("Unexpected underlying type when performing solidity variable analysis.");
        }

        /// <summary>
        /// Obtains a solidity variable type object of the type provided.
        /// </summary>
        /// <param name="astTypeName">The ast node containing the type/location from which we derive our solidity variable type objects from.</param>
        /// <param name="location">The default location that some variable types should be assumed to be at, in this context.</param>
        /// <returns>Returns a solidity variable type object of the type provided by <paramref name="astTypeName"/></returns>
        public static VarBase GetValueParser(AstElementaryTypeName astTypeName, VarLocation location)
        {
            // Obtain our base type.
            string baseType = ParseTypeComponents(astTypeName.TypeDescriptions.TypeString).baseType;

            // Obtain our generic type
            VarGenericType genericType = GetGenericType(baseType);

            // Obtain the variable accordingly.
            switch (genericType)
            {
                case VarGenericType.Array:
                    return new VarArray((AstArrayTypeName)astTypeName, location);
                case VarGenericType.Address:
                    return new VarAddress(astTypeName);
                case VarGenericType.Boolean:
                    return new VarBoolean(astTypeName);
                case VarGenericType.ByteArrayDynamic:
                    return new VarDynamicBytes(astTypeName, location);
                case VarGenericType.ByteArrayFixed:
                    return new VarFixedBytes(astTypeName);
                case VarGenericType.Enum:
                    return new VarEnum((AstUserDefinedTypeName)astTypeName);
                case VarGenericType.String:
                    return new VarString(astTypeName, location);
                case VarGenericType.Struct:
                    return new VarStruct((AstUserDefinedTypeName)astTypeName, location);
                case VarGenericType.Int:
                    return new VarInt(astTypeName);
                case VarGenericType.UInt:
                    return new VarUInt(astTypeName);
                case VarGenericType.Mapping:
                    return new VarMapping((AstMappingTypeName)astTypeName);
                default:
                    // We were unable to obtain our variable object, throw an exception.
                    throw new ArgumentException("Unexpected underlying type when performing solidity variable analysis.");
            }
        }
        #endregion
    }
}
