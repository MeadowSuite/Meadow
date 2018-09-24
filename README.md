[![TeamCity Build Status](https://img.shields.io/teamcity/https/teamcity.meadowsuite.com/e/Meadow_MeadowSuiteTest.svg?label=TeamCity)](https://teamcity.meadowsuite.com/viewType.html?buildTypeId=Meadow_MeadowSuiteTest&guest=1) 
[![Coverage; Report Generator; TeamCity](https://teamcity.meadowsuite.com/repository/download/Meadow_MeadowSuiteTest/lastSuccessful/coverage.zip%21/badge_combined.svg?guest=1)](https://teamcity.meadowsuite.com/viewLog.html?buildId=lastSuccessful&buildTypeId=Meadow_MeadowSuiteTest&tab=report_project2_Code_Coverage&guest=1)
[![Coveralls](https://img.shields.io/coveralls/github/MeadowSuite/Meadow/master.svg?label=Coveralls.io)](https://coveralls.io/github/MeadowSuite/Meadow?branch=master) 
[![codecov](https://img.shields.io/codecov/c/github/MeadowSuite/Meadow/master.svg?lavel=codecov)](https://codecov.io/gh/MeadowSuite/Meadow) 
[![AppVeyor Tests](https://img.shields.io/appveyor/tests/Meadow/Meadow/master.svg?label=AppVeyor%20Tests)](https://ci.appveyor.com/project/Meadow/meadow/branch/master)

# Meadow

An Ethereum implementation geared towards Solidity testing and development. Written in fully cross platform C# with .NET Core.


---

### Meadow.EVM

"Ethereum Core" implementation (pending the in-progress item below). The Ethereum Core can be seen as the Application Layer of Ethereum which constitutes all architectural implementation for the Ethereum Virtual Machine ("EVM"), Transactions, Blocks, Mining, Chains, and all other underlying data types or objects which Ethereum uses and depends on. As such, implementation of a full Ethereum node would simply require writing the remaining Network Layer components, the peer-to-peer networking code to synchronize and interact with other people in the network.

Non-exhaustive list of implemented features:

* The entirety of the EVM: Instructions/Opcodes, Calling Conventions/Messages/Return Values, Memory/Stack, Logs/Events, Gas, Charges/Limits, Precompiles/Precompiled Code (TODO: See below), Contract Creation Logic.
* Core Ethereum Components: Accounts, Account Storage, Transactions, Transaction Receipts, Transaction Pool/Queue, Blocks, World State, World State Snapshoting/Reverting, Chain, Mining/Consensus Mechanism/Scoring/Difficulty/Uncles (Proof of Work only for now, Casper update will soon introduce Proof of Stake)/
* Underlying Dependencies - Datatypes/Providers: Configuration/Genesis Block/Fork/Versioning Support/Chain ID support, Recursive Length Prefix ("RLP") Encoding, Modified Merkle Patricia Tries, Bloom Filters, Elliptic Curve Signing + Public Key Recovery using v, r, s components, Ethereum Hash (Ethash) used for proof of work, with cache/data set generation and signing, Transaction Pool/Queue, Binary Heap (Min/Max Heap), In memory storage database/

---

### Meadow.Core

Core lib referenced by most the others. Contains:

* C# implementations of ethereum/solidity types such as Address, UInt256, Hash, Data, etc.
* ABI utilities such as encoding and decoding between the C# types and ethereum/solidity types.
* Json ABI mapping and parsing utilities.
* Fast hex encoding & decoding for use in JSON-RPC, ABI, etc.
* Fast managed Keccak hash implementation.

### Meadow.JsonRpc

Core json-rpc lib referenced by other libs like the json-rpc server and json-rpc client. Contains:

* Enum list/definitions and C# xmldoc for all RPC methods.
* Interface with all the RPC methods; implemented as idiomatic C# but also closely match the existing Eth RPC definitions and documentation.
* C# class implementations for all the RPC param objects.
* Json.net converter implementations that transparently parse and serialize between C# types and RPC param types on both the client and server boundaries.

---

### Meadow.JsonRpc.Client

Client implementation of the RPC method interface.

Manually implementing the interface requires hundreds of repetitive and error-prone conversions between C# and json-rpc types and would be be annoying to maintain. So instead we use dynamic code generation at runtime which takes the interface definition and produces an implementation that automatically translates calls to the object into json-rpc requests and convert the json-rpc response to C# type return values. 

This lib is used by the generated contract classes.

Future:

* Implement support for the other common json-rpc transport protocols like websocket and IPC

### Meadow.JsonRpc.Server

Lib that provides an http server which routes json-rpc http requests to an instance of the RPC method handler interface, and converts the method invocation results into json-rpc responses sent back to the client.

Uses the fast cross platform ASP.NET Core Kestrel http server.

Future:

* Implement support for the other common json-rpc transport protocols like websocket and IPC

---

### Meadow.JsonRpc.Server.Proxy

.NET Core app that launches a json-rpc http server (using the base Meadow.JsonRpc.Server lib) and proxies rpc calls to another json-rpc server (such as Ganache).

The main purpose is to act as a full end-to-end integration test of the conversions between C# types and json-rpc types. However, can also be used to MTM for debugging and API extension purposes.

---

### Meadow.Contract

Lib that contains helpers and types for interacting with Solidity contracts from C#.
* Contains the BaseContract class and other helpers used by the SolCodeGen code generation tool that takes Solidity source files and generates C# classes corresponding to Solidity contracts. 
* The C# projects pulling in the SolCodeGen tool also get references to this lib.
* Contains lots of helpers so generated contracts can be as simple as possible, such as easily mapping calls to a C# method into RPC calls with correct abi types.
* The dev experience is heavily dependent on the usability of the generated contract classes.

---

### Meadow.SolCodeGen

Tool in the form of a nuget package that is added to C# projects containing solidity source code files.

* The SolCodeGen tool hooks into the build process of the consuming project to compile the solidity source files and generate C# classes corresponding to the Solidity contracts. The generated C# files are added to the consumer project. 
* The process of solidity code compiling to C# contract code generation uses optimized bindings to the native compiled solc lib as well as caching based on file hashes. Its whole process is fast enough to be effectively unnoticeable. 
* The generated contract classes contain:
  * The solc compiled bytecode for the consuming project to easily deploy the contracts.
  * The json-abi data for the developer to use as needed, e.g. : quick view of the ABI when debugging contact, developing helper methods for contracts that need knowledge of the ABI.
  * All public methods and events exposed by the json-abi to corresponding idiomatic C# methods and event log classes. 
* C# testing code simply interacts with the solidity contract represented as regular C# class, and the method invocations do behind the scenes RPC method calls using the JsonRpc.Client, and performs all the type conversions between C# and the json rpc encoded ABI params. 
* Parse event logs from a transaction result and expose to the C# contract class.

Future: 
* Solidity natspec documentation/comments are added to the corresponding C# code as xmldoc that show up in intellisense/tooltips while developing against the contract class. 

---

### Meadow.TestNode

Ethereum "personal blockchain" / "test node" / "RPC Server" / "Ethereum client" ... (Since there is no standard name for this component even though all Ethereum stacks have one, we have decided to call it "node" since it is the least ambiguous of the names used). Other nodes (or stacks containing one) for reference: Ganache (js), Geth (go), Harmony (java), Trinity (python), pyethapp (python), Parity (rust), cpp-ethereum (c++).

This component implements the server handlers for RPC methods, and is ran as either a standalone server or via programmatic setup / teardown during unit test execution. It pulls in the JsonRpc.Server (which only provides http server, RPC method routing and RPC type conversions), and the EVM.

A main purposes of this implementation is to provide the best contract testing and development experience while working with the rest of our stack. It supports transaction & call tracing (for debugging and code coverage) as a first class citizen rather than as a tacked on feature.

Coverage reporting is available via a non-standard RPC call that the client invokes immediately which instructs the node to track coverage globally for the duration of its runtime. Then implement another RPC call that the client invokes at the end of the unit tests which grabs all the coverage data from the server. Coverage data is in the form of opcode offset execution counts and jump data.

Future - Solidity debug architecture:

* The server will use the existing http hosting to provide a secondary debug communication channel side-by-side to the existing RPC http server. It uses websockets for full-duplex communication since the server pushes data to the client.
* This websocket channel is used by the server to receive debug related data from the front-end such as breakpoints, pausing, stepping, etc. The server pushes the resulting debug data back to the client (e.g.: breakpoint halted state, callstack, locals, etc).
* Breakpoint instructions will be given to the server as a message containing the contract address and opcode index.
* While the TestNode EVM is executing transactions, it checks the provided breakpoints for one that involves the currently executing opcode, then pause the execution state, send the related data to the IDE, and wait for further instruction (e.g.: step, continue) from the IDE. 

---

### Meadow.VSCode.SolidityDebugger *(not net implemented)*

VSCode IDE extension that will communicate with the TestNode server over the secondary debug channel to provide all the features for debugging solidity source files. 

Features include: using the regular IDE interface for setting breakpoints in solidity source, pausing, stepping, displaying call stack and locals, etc.

The audit / unit test work flow would like something like:

1. Write a C# tests against the auto generated C# class for a solidity contract (that correspond to solidity contracts). 
2. Set breakpoints in your C# and/or the solidity source code.
3. Run the test, and the IDE will magically treat your C# code and the solidity source files as if they are executing in the same runtime. When a solidity breakpoint hits, it would pause the execution of the C# code, and vice-versa. 

When the user sets a breakpoint, the IDE extension receives a source file and line number message. The extension needs to map the source file to the deployed contract addresses, and map the line number to an opcode index. The data to determine this is available in the solc compilation output.  

VSCode will be our first target for developing this extension. Visual Studio (Windows) and JetBrains Rider are also future targets.

---

### Meadow.CoverageReport

Lib that provides Solidity file test coverage reports.

* Contract deployment/transaction/call execution data from the RPC TestNode is converted into a coverage report format. 
* The coverage data is ran through an aspnetcore Razor html template to generate coverage report pages.

---

### Meadow.UnitTestTemplate

Test harness lib providing seamless integration between MSTest and the .NET solidity tooling stack. Provides a simple workflow where solidity source files can be dropped into a unit test project and C# contract code is automatically generated. Idiomatic C# unit tests can written to easily deploy/call/transact with contracts. RPC test node servers & clients are automatically boostrapped and provided to unit tests. Code coverage reports are automatically generated after unit tests are ran.