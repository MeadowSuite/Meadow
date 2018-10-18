using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Cryptography.ECDSA.Bn128;
using Meadow.Core.Utils;
using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Execution;
using Meadow.EVM.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Meadow.EVM.EVM.Precompiles
{
    /// <summary>
    /// Implements Ethereum Virtual Machine precompiled code, code which is hardcoded to be executed if certain hardcoded addresses are called for code execution.
    /// </summary>
    public abstract class EVMPrecompiles
    {
        #region Fields
        /// <summary>
        /// Precompile address to method handler lookup.
        /// </summary>
        private static ConcurrentDictionary<Address, Func<MeadowEVM, EVMExecutionResult>> _precompiles;
        /// <summary>
        /// SHA256 cryptographic provider used for hashing
        /// </summary>
        private static SHA256 _sha256;
        #endregion

        #region Constructor
        /// <summary>
        /// Static constructor, initializes the Precompiles lookup.
        /// </summary>
        static EVMPrecompiles()
        {
            // Initialize our cryptographic providers.
            _sha256 = SHA256.Create();

            // Initialize the precompile lookup dictionary.
            _precompiles = new ConcurrentDictionary<Address, Func<MeadowEVM, EVMExecutionResult>>();

            // We assign the addresses to the precompile code to run.
            _precompiles["0x01"] = Precompile_ECRecover;
            _precompiles["0x02"] = Precompile_SHA256;
            _precompiles["0x03"] = Precompile_RIPEMD160;
            _precompiles["0x04"] = Precompile_Identity;
            _precompiles["0x05"] = Precompile_ModExp;
            _precompiles["0x06"] = Precompile_ECAdd;
            _precompiles["0x07"] = Precompile_ECMultiply;
            _precompiles["0x08"] = Precompile_ECPairing;
        }
        #endregion

        #region Functions
        // Main Precompiles Methods
        /// <summary>
        /// Checks if a contract address corresponds to a hardcoded precompile code address.
        /// </summary>
        /// <param name="address">The address to check for a precompile at.</param>
        /// <returns>Returns true if this address corresponds to a supported precompile, false otherwise.</returns>
        public static bool IsPrecompileAddress(Address address)
        {
            // Check if this address is in our precompiles lookup.
            return _precompiles.ContainsKey(address);
        }

        /// <summary>
        /// Executes the precompile which corresponds to the given address in our provided EVM instance.
        /// </summary>
        /// <param name="evm">The Ethereum Virtual Machine with which we wish to execute the precompile at the provided address.</param>
        /// <param name="address">The address which corresponds to a precompile we wish to execute.</param>
        public static void ExecutePrecompile(MeadowEVM evm, Address address)
        {
            // We obtain the function which we wish to execute.
            var precompileFunction = _precompiles[address];

            // We execute the precompile and set our execution result.
            evm.ExecutionState.Result = precompileFunction(evm);
        }

        // Precompiles Below
        /// <summary>
        /// A precompiled contract which uses v,r,s + hash obtained from the message data to perform elliptic curve public key recovery to obtain a senders address.
        /// </summary>
        /// <param name="evm">The Ethereum Virtual Machine instance we are executing inside of.</param>
        private static EVMExecutionResult Precompile_ECRecover(MeadowEVM evm)
        {
            // Charge the gas for the precompile operation before processing.
            evm.GasState.Deduct(GasDefinitions.GAS_PRECOMPILE_ECRECOVER);

            // Obtain a memory representation of our data.
            Span<byte> messageData = new Span<byte>(evm.Message.Data);

            // We extract our signature information from message data (256-bit each)
            Span<byte> hash = messageData.Slice(0, 32);
            BigInteger v = BigIntegerConverter.GetBigInteger(messageData.Slice(32, 64));
            BigInteger r = BigIntegerConverter.GetBigInteger(messageData.Slice(64, 96));
            BigInteger s = BigIntegerConverter.GetBigInteger(messageData.Slice(96, 128));

            // Verify we have a low r, s, and a valid v.
            if (r >= Secp256k1Curve.N || s >= Secp256k1Curve.N || v < 27 || v > 28)
            {
                // We failed v,r,s verification, so we stop executing.
               return new EVMExecutionResult(evm, null, true);
            }

            // Obtain our recovery id from v.
            byte recoveryID = EthereumEcdsa.GetRecoveryIDFromV((byte)v);

            // Try to get an address from this. If it fails, it will throw an exception.
            byte[] senderAddress = null;
            try
            {
                senderAddress = EthereumEcdsa.Recover(hash, recoveryID, r, s).GetPublicKeyHash();
            }
            catch
            {
                // Recovery failed, so we stop executing.
                return new EVMExecutionResult(evm, null, true);
            }

            // The address portion is at the end, and we zero out the leading portion.
            for (int i = 0; i < senderAddress.Length - Address.ADDRESS_SIZE; i++)
            {
                senderAddress[i] = 0;
            }

            // Return the sender address
            return new EVMExecutionResult(evm, senderAddress, true);
        }

        /// <summary>
        /// A precompiled contract which computes a SHA256 hash from the message data.
        /// </summary>
        /// <param name="evm">The Ethereum Virtual Machine instance we are executing inside of.</param>
        private static EVMExecutionResult Precompile_SHA256(MeadowEVM evm)
        {
            // Charge the gas for the precompile operation before processing. (A base fee, plus for every word)
            BigInteger gasCharge = GasDefinitions.GAS_PRECOMPILE_SHA256_BASE + (EVMDefinitions.GetWordCount(evm.Message.Data.Length) * GasDefinitions.GAS_PRECOMPILE_SHA256_WORD);
            evm.GasState.Deduct(gasCharge);

            // We compute a hash of the message data.
            byte[] hash = SHA256.Create().ComputeHash(evm.Message.Data);

            // Return the hash
            return new EVMExecutionResult(evm, hash, true);
        }

        /// <summary>
        /// A precompiled contract which computes a RIPEMD160 hash from the message data.
        /// </summary>
        /// <param name="evm">The Ethereum Virtual Machine instance we are executing inside of.</param>
        private static EVMExecutionResult Precompile_RIPEMD160(MeadowEVM evm)
        {
            // Charge the gas for the precompile operation before processing. (A base fee, plus for every word)
            BigInteger gasCharge = GasDefinitions.GAS_PRECOMPILE_RIPEMD160_BASE + (EVMDefinitions.GetWordCount(evm.Message.Data.Length) * GasDefinitions.GAS_PRECOMPILE_RIPEMD160_WORD);
            evm.GasState.Deduct(gasCharge);

            // We compute a hash of the message data.
            byte[] hash = new RIPEMD160Managed().ComputeHash(evm.Message.Data);

            // We make the return value into the size of a word.
            byte[] result = new byte[EVMDefinitions.WORD_SIZE];
            Array.Copy(hash, 0, result, result.Length - hash.Length, hash.Length);

            // Return the result
            return new EVMExecutionResult(evm, result, true);
        }

        /// <summary>
        /// A precompiled contract which acts as memcpy, returning a copy of the message data provided.
        /// </summary>
        /// <param name="evm">The Ethereum Virtual Machine instance we are executing inside of.</param>
        private static EVMExecutionResult Precompile_Identity(MeadowEVM evm)
        {
            // Charge the gas for the precompile operation before processing. (A base fee, plus for every word)
            BigInteger gasCharge = GasDefinitions.GAS_PRECOMPILE_IDENTITY_BASE + (EVMDefinitions.GetWordCount(evm.Message.Data.Length) * GasDefinitions.GAS_PRECOMPILE_IDENTITY_WORD);
            evm.GasState.Deduct(gasCharge);

            // We simply return the data back
            return new EVMExecutionResult(evm, evm.Message.Data, true);
        }

        private static BigInteger Estimate_karatsuba_difficulty(BigInteger x)
        {
            // Estimates the difficulty of karatsuba multiplication, a more efficient multiplication algorithm.
            // Source: https://en.wikipedia.org/wiki/Karatsuba_algorithm
            // As noted by Ethereum, this is a standard function known as "mult_complexity" by all big integer number libraries.
            // Source: https://github.com/ethereum/EIPs/blob/master/EIPS/eip-198.md
            if (x <= 64)
            {
                return BigInteger.Pow(x, 2);
            }
            else if (x <= 1024)
            {
                return (BigInteger.Pow(x, 2) / 4) + (96 * x) - 3072;
            }
            else
            {
                return (BigInteger.Pow(x, 2) / 16) + (480 * x) - 199680;
            }
        }

        private static EVMExecutionResult Precompile_ModExp(MeadowEVM evm)
        {
            // Verify we're past the byzantium fork
            if (evm.Version < EthereumRelease.Byzantium)
            {
                return new EVMExecutionResult(evm, null, true);
            }

            // Source: https://github.com/ethereum/EIPs/blob/master/EIPS/eip-198.md

            // Obtain a memory representation of our data.
            Span<byte> messageData = new Span<byte>(evm.Message.Data);

            // Extract our base length, exponent length, and mod length (in bytes)
            BigInteger baseLength = BigIntegerConverter.GetBigInteger(messageData.Slice(0, EVMDefinitions.WORD_SIZE));
            BigInteger exponentLength = BigIntegerConverter.GetBigInteger(messageData.Slice(32, EVMDefinitions.WORD_SIZE));
            BigInteger modLength = BigIntegerConverter.GetBigInteger(messageData.Slice(64, EVMDefinitions.WORD_SIZE));

            // GAS CALCULATION START: Exponent is leading the word of data, so we obtain a numeric representation of the first bytes.
            BigInteger exponentHead = BigIntegerConverter.GetBigInteger(messageData.Slice(96 + (int)baseLength, EVMDefinitions.WORD_SIZE));

            // Shift our head so we only have relevant bytes (they're leading the word, so we want to cut the tail off by bitshifting).
            exponentHead >>= (8 * (int)BigInteger.Max(32 - exponentLength, 0));

            // Count our bits in our exponent head.
            int exponentHeadBitCount = -1;
            while (exponentHead > 0)
            {
                exponentHead >>= 1;
                exponentHeadBitCount++;
            }

            // Obtain our adjusted exponent length.
            // 1) If exponent length <= 32, and exponent bits are 0, this is 0.
            // 2) If exponent length <= 32, then return the index of the highest bit in exponent.
            // 3) If exponent length > 32, then we return (8 * (exponent-length - 32)) + the index of the highest bit in the exponent.
            BigInteger adjustedExponentLength = Math.Max(exponentHeadBitCount, 0) + (8 * BigInteger.Max(exponentLength - 32, 0));
            adjustedExponentLength = BigInteger.Max(adjustedExponentLength, 1);

            // GAS CALCULATION END: Calculate the final gas cost from the length of our biggest parameter, times the exponent length, divided by our divisor.
            BigInteger biggestLength = BigInteger.Max(modLength, baseLength);
            BigInteger gasCost = (Estimate_karatsuba_difficulty(biggestLength) * adjustedExponentLength) / GasDefinitions.GAS_PRECOMPILE_MODEXP_QUAD_DIVISOR;

            // Deduct our gas cost.
            evm.GasState.Deduct(gasCost);

            // Verify our base length.
            if (baseLength == 0)
            {
                return new EVMExecutionResult(evm, new byte[(int)modLength], true);
            }

            // Verify our mod length.
            if (modLength == 0)
            {
                return new EVMExecutionResult(evm, null, true);
            }

            // Obtain our base, exponent and mod
            Span<byte> memBase = messageData.Slice(96, (int)baseLength);
            BigInteger numBase = BigIntegerConverter.GetBigInteger(memBase, false, memBase.Length);
            Span<byte> memExponent = messageData.Slice(96 + (int)baseLength, (int)exponentLength);
            BigInteger numExponent = BigIntegerConverter.GetBigInteger(memExponent, false, memExponent.Length);
            Span<byte> memMod = messageData.Slice(96 + (int)baseLength + (int)exponentLength, (int)modLength);
            BigInteger numMod = BigIntegerConverter.GetBigInteger(memMod, false, memMod.Length);

            // Verify our divisor isn't 0.
            if (numMod == 0)
            {
                return new EVMExecutionResult(evm, new byte[(int)modLength], true);
            }

            // Obtain our modexp result, which, by definition, we know won't be bigger than our divisor, so we bind our length to the modulo divisor length.
            BigInteger numResult = BigInteger.ModPow(numBase, numExponent, numMod);
            byte[] result = BigIntegerConverter.GetBytes(numResult, (int)modLength);

            // Return our result
            return new EVMExecutionResult(evm, result, true);
        }

        private static FpVector3<Fp> ParsePoint(BigInteger x, BigInteger y)
        {
            // If our coordinates exceed our modulo divisor, they are invalid.
            if (x >= Bn128Curve.P || y >= Bn128Curve.P)
            {
                return null;
            }

            // If our x and y coordinates are 0, we return a static point, otherwise we set components and verify
            FpVector3<Fp> result;
            if (x == 0 && y == 0)
            {
                // Initialize a point with constant values.
                result = new FpVector3<Fp>(Fp.OneValue, Fp.OneValue, Fp.ZeroValue);
            }
            else
            {
                // Initialize a point from our components.
                result = new FpVector3<Fp>(new Fp(x), new Fp(y), Fp.OneValue);

                // Verify this point is on the curve, if not, we return null
                if (!result.IsOnCurveCheck(Bn128Curve.B))
                {
                    result = null;
                }
            }

            // Return the result.
            return result;
        }

        private static EVMExecutionResult Precompile_ECAdd(MeadowEVM evm)
        {
            // Verify we're past the byzantium fork
            if (evm.Version < EthereumRelease.Byzantium)
            {
                return new EVMExecutionResult(evm, null, true);
            }

            // Charge the gas for the precompile operation before processing.
            BigInteger gasCharge = GasDefinitions.GAS_PRECOMPILE_ECADD_BASE;
            evm.GasState.Deduct(gasCharge);

            // Obtain a memory representation of our data.
            Span<byte> messageData = new Span<byte>(evm.Message.Data);

            // Obtain our component data
            BigInteger x1 = BigIntegerConverter.GetBigInteger(messageData.Slice(0, EVMDefinitions.WORD_SIZE));
            BigInteger y1 = BigIntegerConverter.GetBigInteger(messageData.Slice(32, EVMDefinitions.WORD_SIZE));
            BigInteger x2 = BigIntegerConverter.GetBigInteger(messageData.Slice(64, EVMDefinitions.WORD_SIZE));
            BigInteger y2 = BigIntegerConverter.GetBigInteger(messageData.Slice(96, EVMDefinitions.WORD_SIZE));

            // Parse and verify our points.
            FpVector3<Fp> point1 = ParsePoint(x1, y1);
            if (point1 == null)
            {
                throw new EVMException("ECAdd precompile failed because point 1 was deemed invalid when parsing.");
            }

            FpVector3<Fp> point2 = ParsePoint(x2, y2);
            if (point1 == null)
            {
                throw new EVMException("ECAdd precompile failed because point 2 was deemed invalid when parsing.");
            }

            // Add the two points together
            FpVector3<Fp> additionResult = point1.Add(point2);

            // Normalize the result into X/Y components.
            (Fp resultX, Fp resultY) = additionResult.Normalize();

            // Obtain the binary data for these results
            byte[] resultXData = BigIntegerConverter.GetBytes(resultX.N, EVMDefinitions.WORD_SIZE);
            byte[] resultYData = BigIntegerConverter.GetBytes(resultY.N, EVMDefinitions.WORD_SIZE);

            // Concat them to a singular result
            byte[] returnData = resultXData.Concat(resultYData);

            // Return our result
            return new EVMExecutionResult(evm, returnData, true);
        }

        private static EVMExecutionResult Precompile_ECMultiply(MeadowEVM evm)
        {
            // Verify we're past the byzantium fork
            if (evm.Version < EthereumRelease.Byzantium)
            {
                return new EVMExecutionResult(evm, null, true);
            }

            // Charge the gas for the precompile operation before processing.
            BigInteger gasCharge = GasDefinitions.GAS_PRECOMPILE_ECMUL_BASE;
            evm.GasState.Deduct(gasCharge);

            // Obtain a memory representation of our data.
            Span<byte> messageData = new Span<byte>(evm.Message.Data);

            // Obtain our component data
            BigInteger x = BigIntegerConverter.GetBigInteger(messageData.Slice(0, EVMDefinitions.WORD_SIZE));
            BigInteger y = BigIntegerConverter.GetBigInteger(messageData.Slice(32, EVMDefinitions.WORD_SIZE));
            BigInteger multiplier = BigIntegerConverter.GetBigInteger(messageData.Slice(64, EVMDefinitions.WORD_SIZE));

            // Parse and verify our point.
            FpVector3<Fp> point = ParsePoint(x, y);
            if (point == null)
            {
                throw new EVMException("ECMultiply precompile failed because the point was deemed invalid when parsing.");
            }

            // Add the two points together
            FpVector3<Fp> additionResult = point.Multiply(multiplier);

            // Normalize the result into X/Y components.
            (Fp resultX, Fp resultY) = additionResult.Normalize();

            // Obtain the binary data for these results
            byte[] resultXData = BigIntegerConverter.GetBytes(resultX.N, EVMDefinitions.WORD_SIZE);
            byte[] resultYData = BigIntegerConverter.GetBytes(resultY.N, EVMDefinitions.WORD_SIZE);

            // Concat them to a singular result
            byte[] returnData = resultXData.Concat(resultYData);

            // Return our result
            return new EVMExecutionResult(evm, returnData, true);
        }

        private static EVMExecutionResult Precompile_ECPairing(MeadowEVM evm)
        {
            // Verify we're past the byzantium fork
            if (evm.Version < EthereumRelease.Byzantium)
            {
                return new EVMExecutionResult(evm, null, true);
            }

            const int PAIRING_SIZE_PER_POINT = 192;

            // Verify our messatge data is divisible by our size per point.
            if (evm.Message.Data.Length % PAIRING_SIZE_PER_POINT != 0)
            {
                throw new EVMException($"ECPairing precompile failed because the call data was not divisible by the size per point ({PAIRING_SIZE_PER_POINT}).");
            }

            // Charge the gas for the precompile operation before processing.
            BigInteger gasCharge = GasDefinitions.GAS_PRECOMPILE_ECPAIRING_BASE + ((evm.Message.Data.Length / PAIRING_SIZE_PER_POINT) * GasDefinitions.GAS_PRECOMPILE_ECPAIRING_PER_POINT);
            evm.GasState.Deduct(gasCharge);

            // Obtain a memory representation of our data.
            Span<byte> messageData = new Span<byte>(evm.Message.Data);

            // Define some constant variables for arithmetic.
            FpVector3<Fp2> zero = new FpVector3<Fp2>(Fp2.OneValue, Fp2.OneValue, Fp2.ZeroValue);
            Fp12 exponent = Fp12.OneValue;

            // Loop for each point in our data
            for (int i = 0; i < evm.Message.Data.Length; i += PAIRING_SIZE_PER_POINT)
            {
                // Obtain our component data
                BigInteger x1 = BigIntegerConverter.GetBigInteger(messageData.Slice(i + 0, EVMDefinitions.WORD_SIZE));
                BigInteger y1 = BigIntegerConverter.GetBigInteger(messageData.Slice(i + 32, EVMDefinitions.WORD_SIZE));
                BigInteger x2_i = BigIntegerConverter.GetBigInteger(messageData.Slice(i + 64, EVMDefinitions.WORD_SIZE));
                BigInteger x2_r = BigIntegerConverter.GetBigInteger(messageData.Slice(i + 96, EVMDefinitions.WORD_SIZE));
                BigInteger y2_i = BigIntegerConverter.GetBigInteger(messageData.Slice(i + 128, EVMDefinitions.WORD_SIZE));
                BigInteger y2_r = BigIntegerConverter.GetBigInteger(messageData.Slice(i + 160, EVMDefinitions.WORD_SIZE));

                // Parse and verify our point.
                FpVector3<Fp> point1 = ParsePoint(x1, y1);
                if (point1 == null)
                {
                    throw new EVMException("ECPairing precompile failed because point 1 was deemed invalid when parsing.");
                }

                // If our coordinates exceed our modulo divisor, they are invalid.
                if (x2_i >= Bn128Curve.P || x2_r >= Bn128Curve.P || y2_i >= Bn128Curve.P || y2_r >= Bn128Curve.P)
                {
                    throw new EVMException("ECPairing precompile failed because point 2 was deemed invalid when parsing.");
                }

                Fp2 x2 = new Fp2(x2_r, x2_i);
                Fp2 y2 = new Fp2(y2_r, y2_i);
                FpVector3<Fp2> point2 = null;
                if (x2 == Fp2.ZeroValue && y2 == Fp2.ZeroValue)
                {
                    // Our point is the zero point.
                    point2 = zero;
                }
                else
                {
                    // Initialize our point from components.
                    point2 = new FpVector3<Fp2>(x2, y2, Fp2.OneValue);

                    // Verify our desired point is on the curve
                    if (!point2.IsOnCurveCheck(Bn128Curve.B2))
                    {
                        throw new EVMException("ECPairing precompile failed because point 2 was not on the curve.");
                    }
                }

                // Verify multiplying by the curve order is non-zero.
                if (point2.Multiply(Bn128Curve.N).Z != Fp2.ZeroValue)
                {
                    throw new EVMException("ECPairing precompile failed because point 2 was deemed invalid. Points multiplied by the curve order should equal infinity (zero).");
                }

                // Pair our points and multiply the exponent by them.
                exponent *= Bn128Pairing.Pair(point2, point1, false);
            }

            // Return our result
            bool returnStatus = Bn128Pairing.FinalExponentiate(exponent) == Fp12.OneValue;
            byte[] returnData = BigIntegerConverter.GetBytes(returnStatus ? 1 : 0, EVMDefinitions.WORD_SIZE);
            return new EVMExecutionResult(evm, returnData, true);
        }
        #endregion
    }
}
