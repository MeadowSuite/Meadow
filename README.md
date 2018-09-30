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

An integrated Ethereum implementation and tool suite focused on Solidity testing and development. Written completely in cross-platform C# with .NET Core. Meadow can be used in VSCode, Visual Studio, and JetBrains Rider.

## Quick start / guides

* [Writing unit tests](TODO) - getting started writing tests against Solidity contracts and generating code coverage reports.

* [Using the CLI](src/Meadow.Cli/README.md) - contract deployment and interaction against testnode or production.

* [VSCode Solidity Debugger](TODO)

---

#### Powerful Solidity contract development, deployment, and interaction

<img src="/images/screenshot1.png?raw=true" width="700" />

Provides an intuitive framework for writing C# to perform contract deployments, transactions, function calls, RPC requests, and more. Solidity source files are automatically compiled and exposed as C# classes with all contract methods, events, and natspec documentation. Includes a personal Ethereum test node that automatically is setup during test executions.

---

#### Visibility into Solidity revert / exception call stacks

<img src="/images/screenshot2.png?raw=true" width="700" />

Better understanding and investigation of Solidity execution problems. 

---

#### Solidity unit test code coverage HTML reports

<img src="/images/screenshot4.png?raw=true" width="600" />

Perform thorough testing of Solidity codebases. See .sol source code coverage for line, branch, and function execution.

---

# Solidity Debugger

[![vs marketplace](https://img.shields.io/vscode-marketplace/v/hosho.solidity-debugger.svg)](https://marketplace.visualstudio.com/items?itemName=hosho.solidity-debugger)

<img src="/images/screenshot3.png?raw=true" width="800" />

Solidity debugger extension for Visual Studio Code supporting beakpoints, stepping, rewinding, call stacks, local & state variable inspection.

---

# Components

| Library | Package   |  |
|---------|-----------|-------------|
| [Meadow.EVM](src/Meadow.EVM) | [![nuget](https://img.shields.io/nuget/v/Meadow.EVM.svg?colorB=blue)](https://www.nuget.org/packages/Meadow.EVM) | An Ethereum Virtual Machine that includes: <ul><li>Instructions/opcodes, calling conventions/messages/return values, memory/stack, logs/events, gas, charges/limits, precompiles, contract creation logic. <li>Core Ethereum components: account storage, transaction receipts, transaction pool, blocks, world state, snapshoting/reverting, chain, mining/consensus mechanism/scoring/difficulty/uncles. <li>Underlying dependencies: configuration/genesis block/fork/versioning/chain ID support, modified Merkle Patricia Trees, bloom filters, elliptic curve signing + public key recovery, Ethash, in-memory storage database.</ul> |
| [Meadow.TestNode](src/Meadow.TestNode) | [![nuget](https://img.shields.io/nuget/v/Meadow.TestNode.svg?colorB=blue)](https://www.nuget.org/packages/Meadow.TestNode) | Ethereum "personal blockchain" / "test node" / "RPC Server" / "Ethereum client". Ran as either a standalone server or via programmatic setup / teardown during unit test execution. Supports several non-standard RPC methods for debugging, testing, and coverage report generation. |
| [Meadow.SolCodeGen](src/Meadow.SolCodeGen) | [![nuget](https://img.shields.io/nuget/v/Meadow.SolCodeGen.svg?colorB=blue)](https://www.nuget.org/packages/Meadow.SolCodeGen) | Tool that compiles Solidity source files and generates a C# class for each contract. All public methods and events in the contract ABI are translated to corresponding idiomatic C# methods and event log classes. Solidity NatSpec comments / docs are also translated to IntelliSense / code-completion tooltips. This nuget package can be simply added to a project and Solidity files in the project `contracts` directory are automatically compiled. |
| [Meadow.CoverageReport](src/Meadow.CoverageReport) | [![nuget](https://img.shields.io/nuget/v/Meadow.CoverageReport.svg?colorB=blue)](https://www.nuget.org/packages/Meadow.CoverageReport) | Generates HTML and JSON code coverage reports for Solidity source files. Uses execution trace data from the EVM. |
| [Meadow.UnitTestTemplate](src/Meadow.UnitTestTemplate) | [![nuget](https://img.shields.io/nuget/v/Meadow.UnitTestTemplate.svg?colorB=blue)](https://www.nuget.org/packages/Meadow.UnitTestTemplate) | Test harness providing seamless integration between MSTest and Solidity contracts. Provides a simple workflow where Solidity source files are dropped into a unit test project and C# contract code is automatically generated. C# unit tests can easily deploy/call/transact with contracts. RPC test node servers & clients are automatically boostrapped and provided to unit tests. Code coverage reports are automatically generated after unit tests are ran. |
| [Meadow.Cli](src/Meadow.Cli) | [![nuget](https://img.shields.io/nuget/v/Meadow.Cli.svg?colorB=blue)](https://www.nuget.org/packages/Meadow.Cli) | Tool that allows contract deployments and interaction through the command line. Solidity source files are live-compiled using a file system watcher. Can be ran against a automatically bootstrapped test RPC node or an externally configured node. Leverages PowerShell Core to a provide cross platform REPL-like environment with powerful tab-completion when interacting with contracts. |
| [Meadow.Core](src/Meadow.Core) | [![nuget](https://img.shields.io/nuget/v/Meadow.Core.svg?colorB=blue)](https://www.nuget.org/packages/Meadow.Core) | <ul><li>RLP and ABI encoding & decoding utils. <li>Implementations of Ethereum / solidity types such as Address, UInt256, Hash, etc. <li>BIP32, BIP39, BIP44, HD account derivation implementation. <li>Fast managed Keccak hashing. <li> ECDSA / secp256k1 utils.</ul> |
| [Meadow.JsonRpc](src/Meadow.JsonRpc) | [![nuget](https://img.shields.io/nuget/v/Meadow.JsonRpc.svg?colorB=blue)](https://www.nuget.org/packages/Meadow.JsonRpc) | <ul><li>.NET types for the Ethereum JSON-RPC request & response data structures. <li>.NET interface for all RPC methods. <li>Serialization for JSON/hex object formats <-> Solidity/.NET types.</ul> |
| [Meadow.JsonRpc.Client](src/Meadow.JsonRpc.Client) | [![nuget](https://img.shields.io/nuget/v/Meadow.JsonRpc.Client.svg?colorB=blue)](https://www.nuget.org/packages/Meadow.JsonRpc.Client) | JSON-RPC client implementation, supported transports: http, WebSocket, and IPC. |
| [Meadow.JsonRpc.Server](src/Meadow.JsonRpc.Server) | [![nuget](https://img.shields.io/nuget/v/Meadow.JsonRpc.Server.svg?colorB=blue)](https://www.nuget.org/packages/Meadow.JsonRpc.Server) | Fast and lightweight HTTP and WebSockets JSON-RPC server - using Kestrel and managed sockets. |
