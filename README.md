[![Coverage; Report Generator; TeamCity](https://teamcity.meadowsuite.com/repository/download/Meadow_MeadowSuiteTest/lastSuccessful/coverage.zip%21/badge_combined.svg?guest=1)](https://teamcity.meadowsuite.com/viewLog.html?buildId=lastSuccessful&buildTypeId=Meadow_MeadowSuiteTest&tab=report_project2_Code_Coverage&guest=1)
[![Coveralls](https://img.shields.io/coveralls/github/MeadowSuite/Meadow/master.svg?label=Coveralls.io)](https://coveralls.io/github/MeadowSuite/Meadow?branch=master) 
[![codecov](https://img.shields.io/codecov/c/github/MeadowSuite/Meadow/master.svg?label=Codecov.io)](https://codecov.io/gh/MeadowSuite/Meadow) 
[![AppVeyor Tests](https://img.shields.io/appveyor/tests/Meadow/Meadow/master.svg?label=AppVeyor%20Tests)](https://ci.appveyor.com/project/Meadow/meadow/branch/master) 
[![Gitter](https://img.shields.io/gitter/room/MeadowSuite/Meadow.svg?label=Chat)](https://gitter.im/MeadowSuite/Lobby)

| Platform | Build |
|----------|-------|
| Windows x64 | [![Build Status](https://ci.appveyor.com/api/projects/status/nauu7pmvu9q2b2xd/branch/master?svg=true)](https://ci.appveyor.com/project/Meadow/meadow/branch/master) |
| Linux x64 | [![Build Status](https://img.shields.io/teamcity/https/teamcity.meadowsuite.com/e/Meadow_MeadowSuiteTest.svg)](https://teamcity.meadowsuite.com/viewType.html?buildTypeId=Meadow_MeadowSuiteTest&guest=1)  |
| MacOS x64 | [![Build Status](https://travis-ci.com/MeadowSuite/Meadow.svg?branch=master)](https://travis-ci.com/MeadowSuite/Meadow) |

# Meadow

An Ethereum implementation geared towards Solidity testing and development. Written in fully cross platform C# with .NET Core.


### Powerful Solidity contract development, deployment, and interaction

(screenshot of C# test method doing contract deployment, transaction, and event log checking - showing intellisense for contract methods)

* Includes a personal Ethereum test node that is automatically setup during test executions.
* Includes an intuitive framework for writing C# tests against contract deployments and interactions.
* Integrated Solidity debugger for investigating a Solidity stack trace leading to a revert.
  (screenshot of revert callstack)
* VSCode Solidity debugger extension for breakpoints, stepping, and variable inspection.
  (screenshot of VSCode breakpoint debugging)
* Solidity code coverage HTML and JSON reports generated after unit test runs.
  (screenshot of coverage report)

## Quick start / guides

* [Writing unit tests](TODO) - getting started writing tests against Solidity contracts and generating code coverage reports.

* [Using the CLI](Meadow.Cli/README.md) - contract deployment and interaction against testnode or production.

* [VSCode Solidity Debugger](TODO)

---

# Components

| Library | Package |  |
|---------|---------|-------------|
| [Meadow.EVM](Meadow.EVM) | nuget | An Ethereum Virtual Machine that includes: <ul><li>Instructions/opcodes, calling conventions/messages/return values, memory/stack, logs/events, gas, charges/limits, precompiles, contract creation logic. <li>Core Ethereum components: account storage, transaction receipts, transaction pool, blocks, world state, snapshoting/reverting, chain, mining/consensus mechanism/scoring/difficulty/uncles. <li>Underlying dependencies: configuration/genesis block/fork/versioning/chain ID support, modified Merkle Patricia Tries, bloom filters, elliptic curve signing + public key recovery, Ethash, in-memory storage database.</ul> |
| [Meadow.TestNode](Meadow.TestNode) | nuget | Ethereum "personal blockchain" / "test node" / "RPC Server" / "Ethereum client". Ran as either a standalone server or via programmatic setup / teardown during unit test execution. Supports several non-standard RPC methods for debugging, testing, and coverage report generation. |
| [Meadow.SolCodeGen](Meadow.SolCodeGen) | nuget | Tool that compiles Solidity source files and generates a C# class for each contract. All public methods and events in the contract ABI are translated to corresponding idiomatic C# methods and event log classes. Solidity NatSpec comments / docs are also translated to IntelliSense / code-completion tooltips. This nuget package can be simply added to a project and Solidity files in the project `contracts` directory are automatically compiled. |
| [Meadow.CoverageReport](Meadow.CoverageReport) | nuget | Generates HTML and JSON code coverage reports for Solidity source files. Uses execution trace data from the EVM. |
| [Meadow.UnitTestTemplate](Meadow.UnitTestTemplate) | nuget | Test harness providing seamless integration between MSTest and Solidity contracts. Provides a simple workflow where Solidity source files are dropped into a unit test project and C# contract code is automatically generated. C# unit tests can easily deploy/call/transact with contracts. RPC test node servers & clients are automatically boostrapped and provided to unit tests. Code coverage reports are automatically generated after unit tests are ran. |
| [Meadow.Cli](Meadow.Cli) | nuget | Tool that allows contract deployments and interaction through the command line. Solidity source files are live-compiled using a file system watcher. Can be ran against a automatically bootstrapped test RPC node or an externally configured node. Leverages PowerShell Core to a provide cross platform REPL-like environment with powerful tab-completion when interacting with contracts. |
| [Meadow.VSCode.Debugger](Meadow.VSCode.Debugger) | nuget | Solidity debugger extension for VSCode supporting beakpoints, stepping, rewinding, call stacks, local & state variable inspection. |
| [Meadow.DebugAdapterServer](Meadow.DebugAdapterServer) | nuget | TODO description... |
| [Meadow.Core](Meadow.Core) | nuget | <ul><li>RLP and ABI encoding & decoding utils. <li>Implementations of Ethereum / solidity types such as Address, UInt256, Hash, etc. <li>BIP32, BIP39, BIP44, HD account derivation implementation. <li>Fast managed Keccak hashing. <li> ECDSA / secp256k1 utils.</ul> |
| [Meadow.JsonRpc](Meadow.JsonRpc) | nuget | <ul><li>.NET types for the Ethereum JSON-RPC request & response data structures. <li>.NET interface for all RPC methods. <li>Serialization for JSON/hex object formats <-> Solidity/.NET types.</ul> |
| [Meadow.JsonRpc.Client](Meadow.JsonRpc.Client) | nuget | JSON-RPC client implementation, supported transports: http, WebSocket, and IPC. |
| [Meadow.JsonRpc.Server](Meadow.JsonRpc.Server) | nuget | Fast and lightweight JSON-RPC HTTP server using Kestrel and managed sockets. |