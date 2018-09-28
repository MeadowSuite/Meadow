using Meadow.EVM.Configuration;
using Meadow.EVM.EVM.Instructions.Arithmetic;
using Meadow.EVM.EVM.Instructions.Bitwise_Logic;
using Meadow.EVM.EVM.Instructions.Block_Information;
using Meadow.EVM.EVM.Instructions.Control_Flow_and_IO;
using Meadow.EVM.EVM.Instructions.Cryptography;
using Meadow.EVM.EVM.Instructions.Environment;
using Meadow.EVM.EVM.Instructions.Stack;
using Meadow.EVM.EVM.Instructions.System_Operations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;

namespace Meadow.EVM.EVM.Instructions
{
    /*
     * Source: https://ethereum.github.io/yellowpaper/paper.pdf (Begins at page 28/39)
     * */
    public enum InstructionOpcode : byte
    {
        #region 0s: Stop and Arithmetic Operations
        [OpcodeDescriptor(typeof(InstructionHalt), "STOP", 0, 0, 0, "Halts execution")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 0)]
        STOP = 0x00,
        [OpcodeDescriptor(typeof(InstructionAdd), "ADD", 0, 2, 1, "Addition operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        ADD = 0x01,
        [OpcodeDescriptor(typeof(InstructionMultiply), "MUL", 0, 2, 1, "Multiplication operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 5)]
        MUL = 0x02,
        [OpcodeDescriptor(typeof(InstructionSubtract), "SUB", 0, 2, 1, "Subtraction operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SUB = 0x03,
        [OpcodeDescriptor(typeof(InstructionDivide), "DIV", 0, 2, 1, "Integer division operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 5)]
        DIV = 0x04,
        [OpcodeDescriptor(typeof(InstructionSignedDivide), "SDIV", 0, 2, 1, "Signed integer division operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 5)]
        SDIV = 0x05,
        [OpcodeDescriptor(typeof(InstructionMod), "MOD", 0, 2, 1, "Modulo remainder operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 5)]
        MOD = 0x06,
        [OpcodeDescriptor(typeof(InstructionSignedMod), "SMOD", 0, 2, 1, "Signed modulo remainder operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 5)]
        SMOD = 0x07,
        [OpcodeDescriptor(typeof(InstructionAddMod), "ADDMOD", 0, 3, 1, "Modulo addition operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 8)]
        ADDMOD = 0x08,
        [OpcodeDescriptor(typeof(InstructionMultiplyMod), "MULMOD", 0, 3, 1, "Modulo multiplication operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 8)]
        MULMOD = 0x09,
        [OpcodeDescriptor(typeof(InstructionExponent), "EXP", 0, 2, 1, "Exponential operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 10)]
        EXP = 0x0a,
        [OpcodeDescriptor(typeof(InstructionSignExtend), "SIGNEXTEND", 0, 2, 1, "Extend length of two's complement signed integer")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 5)]
        SIGNEXTEND = 0x0b,
        #endregion

        #region 10s: Comparison & Bitwise Logic Operations
        [OpcodeDescriptor(typeof(InstructionLessThan), "LT", 0, 2, 1, "Less than comparison")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        LT = 0x10,
        [OpcodeDescriptor(typeof(InstructionGreaterThan), "GT", 0, 2, 1, "Greater-than comparison")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        GT = 0x11,
        [OpcodeDescriptor(typeof(InstructionSignedLessThan), "SLT", 0, 2, 1, "Signed less-than comparison")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SLT = 0x12,
        [OpcodeDescriptor(typeof(InstructionSignedGreaterThan), "SGT", 0, 2, 1, "Signed greater-than comparison")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SGT = 0x13,
        [OpcodeDescriptor(typeof(InstructionEqual), "EQ", 0, 2, 1, "Equality comparison")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        EQ = 0x14,
        [OpcodeDescriptor(typeof(InstructionIsZero), "ISZERO", 0, 1, 1, "Simple not operator")] // a unary NOT operation (true or false only)
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        ISZERO = 0x15,
        [OpcodeDescriptor(typeof(InstructionAnd), "AND", 0, 2, 1, "Bitwise AND operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        AND = 0x16,
        [OpcodeDescriptor(typeof(InstructionOr), "OR", 0, 2, 1, "Bitwise OR operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        OR = 0x17,
        [OpcodeDescriptor(typeof(InstructionXor), "XOR", 0, 2, 1, "Bitwise XOR operation")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        XOR = 0x18,
        [OpcodeDescriptor(typeof(InstructionNot), "NOT", 0, 1, 1, "Bitwise NOT operation")] // a bitwise NOT / negation
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        NOT = 0x19,
        [OpcodeDescriptor(typeof(InstructionExtractByte), "BYTE", 0, 2, 1, "Retrieve single byte from word")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        BYTE = 0x1a,
        #endregion

        #region 20s: Cryptographic Hashes
        [OpcodeDescriptor(typeof(InstructionSHA3), "SHA3", 0, 2, 1, "Compute Keccak-256 hash")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 30)]
        SHA3 = 0x20,
        #endregion

        #region 30s: Environment Variables
        [OpcodeDescriptor(typeof(InstructionAddress), "ADDRESS", 0, 0, 1, "Get address of currently executing account")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        ADDRESS = 0x30,
        [OpcodeDescriptor(typeof(InstructionBalance), "BALANCE", 0, 1, 1, "Get balance of the given account")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 20), OpcodeBaseGasCost(EthereumRelease.TangerineWhistle, 400)]
        BALANCE = 0x31,
        [OpcodeDescriptor(typeof(InstructionOrigin), "ORIGIN", 0, 0, 1, "Get execution origination address")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        ORIGIN = 0x32,
        [OpcodeDescriptor(typeof(InstructionCaller), "CALLER", 0, 0, 1, "Get caller address")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        CALLER = 0x33,
        [OpcodeDescriptor(typeof(InstructionCallValue), "CALLVALUE", 0, 0, 1, "Get deposited value by the instruction/transaction responsible for this execution")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        CALLVALUE = 0x34,
        [OpcodeDescriptor(typeof(InstructionCallDataLoad), "CALLDATALOAD", 0, 1, 1, "Get input data of current environment")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        CALLDATALOAD = 0x35,
        [OpcodeDescriptor(typeof(InstructionCallDataSize), "CALLDATASIZE", 0, 0, 1, "Get size of input data in current environment")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        CALLDATASIZE = 0x36,
        [OpcodeDescriptor(typeof(InstructionCallDataCopy), "CALLDATACOPY", 0, 3, 0, "Copy input data in current environment to memory")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        CALLDATACOPY = 0x37,
        [OpcodeDescriptor(typeof(InstructionCodeSize), "CODESIZE", 0, 0, 1, "Get size of code running in current environment")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        CODESIZE = 0x38,
        [OpcodeDescriptor(typeof(InstructionCodeCopy), "CODECOPY", 0, 3, 0, "Copy code running in current environment to memory")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        CODECOPY = 0x39,
        [OpcodeDescriptor(typeof(InstructionGasPrice), "GASPRICE", 0, 0, 1, "Get price of gas in current environment")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        GASPRICE = 0x3a,
        [OpcodeDescriptor(typeof(InstructionExternalCodeSize), "EXTCODESIZE", 0, 1, 1, "Get size of an account's code")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 20), OpcodeBaseGasCost(EthereumRelease.TangerineWhistle, 700)]
        EXTCODESIZE = 0x3b,
        [OpcodeDescriptor(typeof(InstructionExternalCodeCopy), "EXTCODECOPY", 0, 4, 0, "Copy an account's code to memory")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 20), OpcodeBaseGasCost(EthereumRelease.TangerineWhistle, 700)]
        EXTCODECOPY = 0x3c,
        [OpcodeDescriptor(typeof(InstructionReturnDataSize), "RETURNDATASIZE", 0, 0, 1, "Get size of output data from the previous call from the current environment")]
        [OpcodeBaseGasCost(EthereumRelease.Byzantium, 2)]
        RETURNDATASIZE = 0x3d,
        [OpcodeDescriptor(typeof(InstructionReturnDataCopy), "RETURNDATACOPY", 0, 3, 0, "Copy output data from the previous call to memory")]
        [OpcodeBaseGasCost(EthereumRelease.Byzantium, 3)]
        RETURNDATACOPY = 0x3e,
        #endregion

        #region 40s: Block Information
        [OpcodeDescriptor(typeof(InstructionBlockHash), "BLOCKHASH", 0, 1, 1, "Get the hash of one of the 256 most recent complete blocks")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 20)]
        BLOCKHASH = 0x40,
        [OpcodeDescriptor(typeof(InstructionBlockCoinbase), "COINBASE", 0, 0, 1, "Get the block's beneficiary address")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        COINBASE = 0x41,
        [OpcodeDescriptor(typeof(InstructionBlockTimestamp), "TIMESTAMP", 0, 0, 1, "Get the block's timestamp")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        TIMESTAMP = 0x42,
        [OpcodeDescriptor(typeof(InstructionBlockNumber), "NUMBER", 0, 0, 1, "Get the block's number")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        NUMBER = 0x43,
        [OpcodeDescriptor(typeof(InstructionBlockDifficulty), "DIFFICULTY", 0, 0, 1, "Get the block's difficulty")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        DIFFICULTY = 0x44,
        [OpcodeDescriptor(typeof(InstructionBlockGasLimit), "GASLIMIT", 0, 0, 1, "Get the block's gas limit")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        GASLIMIT = 0x45,
        #endregion

        #region 50s: Stack, Memory, Storage and Flow Operations
        [OpcodeDescriptor(typeof(InstructionPop), "POP", 0, 1, 0, "Remove item from stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        POP = 0x50,
        [OpcodeDescriptor(typeof(InstructionMemoryLoad), "MLOAD", 0, 1, 1, "Load word from memory")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        MLOAD = 0x51,
        [OpcodeDescriptor(typeof(InstructionMemoryStore), "MSTORE", 0, 2, 0, "Save word to memory")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        MSTORE = 0x52,
        [OpcodeDescriptor(typeof(InstructionMemoryStore8), "MSTORE8", 0, 2, 0, "Save byte to memory")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        MSTORE8 = 0x53,
        [OpcodeDescriptor(typeof(InstructionStorageLoad), "SLOAD", 0, 1, 1, "Load word from storage")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 50), OpcodeBaseGasCost(EthereumRelease.TangerineWhistle, 200)]
        SLOAD = 0x54,
        [OpcodeDescriptor(typeof(InstructionStorageStore), "SSTORE", 0, 2, 0, "Save word to storage")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 0)]
        SSTORE = 0x55,
        [OpcodeDescriptor(typeof(InstructionJump), "JUMP", 0, 1, 0, "Alter the program counter")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 8)]
        JUMP = 0x56,
        [OpcodeDescriptor(typeof(InstructionJumpI), "JUMPI", 0, 2, 0, "Conditionally alter the program counter")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 10)]
        JUMPI = 0x57,
        [OpcodeDescriptor(typeof(InstructionGetPC), "PC", 0, 0, 1, "Get the value of the program counter prior to the increment corresponding to this instruction")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        PC = 0x58,
        [OpcodeDescriptor(typeof(InstructionMemorySize), "MSIZE", 0, 0, 1, "Get the size of active memory in bytes")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        MSIZE = 0x59,
        [OpcodeDescriptor(typeof(InstructionGas), "GAS", 0, 0, 1, "Get the amount of available gas, including the corresponding reduction for the cost of this instruction")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 2)]
        GAS = 0x5a,
        [OpcodeDescriptor(typeof(InstructionJumpDest), "JUMPDEST", 0, 0, 0, "Mark a valid destination for jumps")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 1)]
        JUMPDEST = 0x5b,
        #endregion

        #region 60s & 70s: Push Operations
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH1", 1, 0, 1, "Place 1 byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH1 = 0x60,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH2", 2, 0, 1, "Place 2-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH2 = 0x61,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH3", 3, 0, 1, "Place 3-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH3 = 0x62,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH4", 4, 0, 1, "Place 4-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH4 = 0x63,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH5", 5, 0, 1, "Place 5-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH5 = 0x64,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH6", 6, 0, 1, "Place 6-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH6 = 0x65,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH7", 7, 0, 1, "Place 7-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH7 = 0x66,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH8", 8, 0, 1, "Place 8-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH8 = 0x67,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH9", 9, 0, 1, "Place 9-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH9 = 0x68,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH10", 10, 0, 1, "Place 10-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH10 = 0x69,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH11", 11, 0, 1, "Place 11-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH11 = 0x6a,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH12", 12, 0, 1, "Place 12-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH12 = 0x6b,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH13", 13, 0, 1, "Place 13-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH13 = 0x6c,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH14", 14, 0, 1, "Place 14-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH14 = 0x6d,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH15", 15, 0, 1, "Place 15-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH15 = 0x6e,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH16", 16, 0, 1, "Place 16-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH16 = 0x6f,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH17", 17, 0, 1, "Place 17-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH17 = 0x70,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH18", 18, 0, 1, "Place 18-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH18 = 0x71,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH19", 19, 0, 1, "Place 19-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH19 = 0x72,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH20", 20, 0, 1, "Place 20-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH20 = 0x73,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH21", 21, 0, 1, "Place 21-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH21 = 0x74,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH22", 22, 0, 1, "Place 22-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH22 = 0x75,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH23", 23, 0, 1, "Place 23-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH23 = 0x76,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH24", 24, 0, 1, "Place 24-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH24 = 0x77,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH25", 25, 0, 1, "Place 25-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH25 = 0x78,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH26", 26, 0, 1, "Place 26-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH26 = 0x79,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH27", 27, 0, 1, "Place 27-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH27 = 0x7a,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH28", 28, 0, 1, "Place 28-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH28 = 0x7b,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH29", 29, 0, 1, "Place 29-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH29 = 0x7c,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH30", 30, 0, 1, "Place 30-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH30 = 0x7d,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH31", 31, 0, 1, "Place 31-byte item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH31 = 0x7e,
        [OpcodeDescriptor(typeof(InstructionPush), "PUSH32", 32, 0, 1, "Place 32-byte (full word) item on stack")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        PUSH32 = 0x7f,
        #endregion

        #region 80s: Duplication Operations:
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP1", 0, 1, 2, "Duplicate 1st stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP1 = 0x80,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP2", 0, 2, 3, "Duplicate 2nd stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP2 = 0x81,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP3", 0, 3, 4, "Duplicate 3rd stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP3 = 0x82,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP4", 0, 4, 5, "Duplicate 4th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP4 = 0x83,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP5", 0, 5, 6, "Duplicate 5th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP5 = 0x84,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP6", 0, 6, 7, "Duplicate 6th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP6 = 0x85,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP7", 0, 7, 8, "Duplicate 7th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP7 = 0x86,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP8", 0, 8, 9, "Duplicate 8th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP8 = 0x87,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP9", 0, 9, 10, "Duplicate 9th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP9 = 0x88,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP10", 0, 10, 11, "Duplicate 10th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP10 = 0x89,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP11", 0, 11, 12, "Duplicate 11th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP11 = 0x8a,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP12", 0, 12, 13, "Duplicate 12th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP12 = 0x8b,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP13", 0, 13, 14, "Duplicate 13th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP13 = 0x8c,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP14", 0, 14, 15, "Duplicate 14th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP14 = 0x8d,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP15", 0, 15, 16, "Duplicate 15th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP15 = 0x8e,
        [OpcodeDescriptor(typeof(InstructionDuplicate), "DUP16", 0, 16, 17, "Duplicate 16th stack item")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        DUP16 = 0x8f,
        #endregion

        #region 90s: Swap Operations:
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP1", 0, 2, 2, "Exchanges 1st and 2nd stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP1 = 0x90,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP2", 0, 3, 3, "Exchanges 1st and 3rd stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP2 = 0x91,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP3", 0, 4, 4, "Exchanges 1st and 4th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP3 = 0x92,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP4", 0, 5, 5, "Exchanges 1st and 5th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP4 = 0x93,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP5", 0, 6, 6, "Exchanges 1st and 6th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP5 = 0x94,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP6", 0, 7, 7, "Exchanges 1st and 7th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP6 = 0x95,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP7", 0, 8, 8, "Exchanges 1st and 8th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP7 = 0x96,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP8", 0, 9, 9, "Exchanges 1st and 9th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP8 = 0x97,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP9", 0, 10, 10, "Exchanges 1st and 10th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP9 = 0x98,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP10", 0, 11, 11, "Exchanges 1st and 11th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP10 = 0x99,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP11", 0, 12, 12, "Exchanges 1st and 12th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP11 = 0x9a,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP12", 0, 13, 13, "Exchanges 1st and 13th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP12 = 0x9b,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP13", 0, 14, 14, "Exchanges 1st and 14th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP13 = 0x9c,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP14", 0, 15, 15, "Exchanges 1st and 15th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP14 = 0x9d,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP15", 0, 16, 16, "Exchanges 1st and 16th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP15 = 0x9e,
        [OpcodeDescriptor(typeof(InstructionSwap), "SWAP16", 0, 17, 17, "Exchanges 1st and 17th stack items")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 3)]
        SWAP16 = 0x9f,
        #endregion

        #region a0s: Log Operations:
        [OpcodeDescriptor(typeof(InstructionLog), "LOG0", 0, 2, 0, "Append log record with no topics")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 375)]
        LOG0 = 0xa0,
        [OpcodeDescriptor(typeof(InstructionLog), "LOG1", 0, 3, 0, "Append log record with one topic")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 750)]
        LOG1 = 0xa1,
        [OpcodeDescriptor(typeof(InstructionLog), "LOG2", 0, 4, 0, "Append log record with two topics")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 1125)]
        LOG2 = 0xa2,
        [OpcodeDescriptor(typeof(InstructionLog), "LOG3", 0, 5, 0, "Append log record with three topics")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 1500)]
        LOG3 = 0xa3,
        [OpcodeDescriptor(typeof(InstructionLog), "LOG4", 0, 6, 0, "Append log record with four topics")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 1875)]
        LOG4 = 0xa4,
        #endregion

        #region e0s: ???
        /*
         * The following are to be discontinued:
         * */
        //SLOADBYTES = 0xe1,
        //SSTOREBYTES = 0xe2,
        //SSIZE = 0xe3,
        #endregion

        #region f0s: System Operations
        [OpcodeDescriptor(typeof(InstructionCreate), "CREATE", 0, 3, 1, "Create a new account with associated code")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 32000)]
        CREATE = 0xf0,
        [OpcodeDescriptor(typeof(InstructionCall), "CALL", 0, 7, 1, "Message-call into an account")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 40), OpcodeBaseGasCost(EthereumRelease.TangerineWhistle, 700)]
        CALL = 0xf1,
        [OpcodeDescriptor(typeof(InstructionCall), "CALLCODE", 0, 7, 1, "Message-call into this account with an alternative account's code")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 40), OpcodeBaseGasCost(EthereumRelease.TangerineWhistle, 700)]
        CALLCODE = 0xf2,
        [OpcodeDescriptor(typeof(InstructionReturn), "RETURN", 0, 2, 0, "Halt execution returning output data")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 0)]
        RETURN = 0xf3,
        [OpcodeDescriptor(typeof(InstructionCall), "DELEGATECALL", 0, 6, 1, "Message-call into this account with an alternative account's code, but persisting the current values for sender and value")]
        [OpcodeBaseGasCost(EthereumRelease.Homestead, 40), OpcodeBaseGasCost(EthereumRelease.TangerineWhistle, 700)]
        DELEGATECALL = 0xf4,
        [OpcodeDescriptor(typeof(InstructionCall), "STATICCALL", 0, 6, 1, "Static message-call into an account")]
        [OpcodeBaseGasCost(EthereumRelease.Byzantium, 700)]
        STATICCALL = 0xfa,
        [OpcodeDescriptor(typeof(InstructionRevert), "REVERT", 0, 2, 0, "Halt execution reverting state changes but returning data and remaining gas")]
        [OpcodeBaseGasCost(EthereumRelease.Byzantium, 0)]
        REVERT = 0xfd,
        #endregion

        #region ff: Halt Execution, Register for Deletion
        [OpcodeDescriptor(typeof(InstructionInvalid), "INVALID", 0, 0, 0, "Designated invalid instruction")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 0)]
        INVALID = 0xfe,
        [OpcodeDescriptor(typeof(InstructionSelfDestruct), "SELFDESTRUCT", 0, 1, 0, "Halt execution and register account for later deletion")]
        [OpcodeBaseGasCost(EthereumRelease.Frontier, 0), OpcodeBaseGasCost(EthereumRelease.TangerineWhistle, 5000)]
        SELFDESTRUCT = 0xff
        #endregion
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class OpcodeDescriptorAttribute : System.Attribute
    {
        #region Properties
        /// <summary>
        /// A symbol describing the opcode.
        /// </summary>
        public string Mnemonic { get; }
        /// <summary>
        /// A short description of the opcode.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// The size of the instruction's operand in bytes.
        /// </summary>
        public uint OperandSize { get; }
        /// <summary>
        /// The amount of items this operation adds to the stack.
        /// </summary>
        public uint ItemsAddedToStack { get; }
        /// <summary>
        /// The amount of items this operation removes from the stack.
        /// </summary>
        public uint ItemsRemovedFromStack { get; }
        /// <summary>
        /// The Type of the class where the instruction is implemented, used to instantiate the instruction.
        /// </summary>
        public Type ImplementationClassType { get; }
        #endregion

        #region Constructors
        public OpcodeDescriptorAttribute(Type implementationClassType, string mnemonic, uint operandSize, uint itemsRemovedFromStack, uint itemsAddedToStack) : this(implementationClassType, mnemonic, operandSize, itemsRemovedFromStack, itemsAddedToStack, null) { }
        public OpcodeDescriptorAttribute(Type implementationClassType, string mnemonic, uint operandSize, uint itemsRemovedFromStack, uint itemsAddedToStack, string description)
        {
            // Set our description data
            ImplementationClassType = implementationClassType;
            Mnemonic = mnemonic;
            OperandSize = operandSize;
            ItemsAddedToStack = itemsAddedToStack;
            ItemsRemovedFromStack = itemsRemovedFromStack;
            Description = description;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Creates an instance of the appropriate instruction implementation for this opcode.
        /// </summary>
        /// <returns>Returns the instruction implementation.</returns>
        public InstructionBase GetInstructionImplementation(MeadowEVM evm)
        {
            // Obtain our constructor for this instruction implementation
            ConstructorInfo constructorInfo = ImplementationClassType.GetConstructor(new Type[] { typeof(MeadowEVM) });

            // Invoke the constructor with the appropriate arguments.
            return (InstructionBase)constructorInfo.Invoke(new object[] { evm });
        }
        #endregion
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class OpcodeBaseGasCostAttribute : System.Attribute
    {
        #region Properties
        public EthereumRelease Version { get; }
        public uint BaseGasCost { get; }
        #endregion

        #region Constructors
        public OpcodeBaseGasCostAttribute(EthereumRelease version, uint baseGasCost)
        {
            Version = version;
            BaseGasCost = baseGasCost;
        }
        #endregion
    }

    public static class InstructionOpcodeExtensions
    {
        #region Fields
        private static ConcurrentDictionary<InstructionOpcode, OpcodeDescriptorAttribute> _cachedDescriptors;
        #endregion

        #region Constructor
        static InstructionOpcodeExtensions()
        {
            // Initialize our descriptor cache.
            _cachedDescriptors = new ConcurrentDictionary<InstructionOpcode, OpcodeDescriptorAttribute>();
        }
        #endregion

        #region Functions
        public static OpcodeDescriptorAttribute GetDescriptor(this InstructionOpcode opcode)
        {
            // Check our cache for our descriptor.
            if (_cachedDescriptors.TryGetValue(opcode, out var val))
            {
                return val;
            }

            // Cache miss, obtain our enum option's field info
            FieldInfo fi = opcode.GetType().GetField(opcode.ToString());
            if (fi == null)
            {
                return null;
            }

            // Obtain all attributes of type we are interested in.
            var attributes = fi.GetCustomAttributes<OpcodeDescriptorAttribute>(false).ToArray();

            // If one exists, cache it and return it.
            if (attributes != null && attributes.Length > 0)
            {
                _cachedDescriptors[opcode] = attributes[0];
                return attributes[0];
            }

            return null;
        }

        public static OpcodeBaseGasCostAttribute[] GetBaseGasCosts(this InstructionOpcode opcode)
        {
            // Cache miss, obtain our enum option's field info
            FieldInfo fi = opcode.GetType().GetField(opcode.ToString());
            if (fi == null)
            {
                return null;
            }

            // Obtain all attributes of type we are interested in.
            var attributes = fi.GetCustomAttributes<OpcodeBaseGasCostAttribute>(false).ToArray();
            return attributes;
        }
        #endregion
    }
}
