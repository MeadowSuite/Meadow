# Meadow

An Ethereum implementation and tool suite designed for Solidity testing and development. The unit testing framework provides fast parallelized test execution, built-in code coverage reporting, strongly typed Contract interfaces with powerful code-completion, Solidity stacktraces for reverts and exceptions, breakpoint debugging and more. 

Written completely in cross-platform C# with .NET Core. Meadow can be used in [VS Code](https://code.visualstudio.com/), [Visual Studio](https://visualstudio.microsoft.com/vs/), and [JetBrains Rider](https://www.jetbrains.com/rider/).

[![Coverage; Report Generator; TeamCity](http://192.241.156.100:8111/repository/download/Meadow_MeadowSuiteTest/lastSuccessful/coverage.zip%21/badge_combined.svg?guest=1)](http://192.241.156.100:8111/viewLog.html?buildId=lastSuccessful&buildTypeId=Meadow_MeadowSuiteTest&tab=report_project2_Code_Coverage&guest=1)
[![Coveralls](https://img.shields.io/coveralls/github/MeadowSuite/Meadow/master.svg?label=Coveralls.io)](https://coveralls.io/github/MeadowSuite/Meadow?branch=master) 
[![codecov](https://img.shields.io/codecov/c/github/MeadowSuite/Meadow/master.svg?label=Codecov.io)](https://codecov.io/gh/MeadowSuite/Meadow) 
[![AppVeyor Tests](https://img.shields.io/appveyor/tests/Meadow/Meadow/master.svg?label=AppVeyor%20Tests)](https://ci.appveyor.com/project/Meadow/meadow/branch/master) 
[![Gitter](https://img.shields.io/gitter/room/MeadowSuite/Meadow.svg?label=Chat)](https://gitter.im/MeadowSuite/Lobby)


<table>
  <tr>
    <td rowspan="2">Builds</td>
    <td>Windows Status</td>
    <td>MacOS Status</td>
  </tr>
  <tr>
    <td><a href="https://ci.appveyor.com/project/Meadow/meadow/branch/master"><img src="https://ci.appveyor.com/api/projects/status/nauu7pmvu9q2b2xd/branch/master?svg=true"></a></td>
    <td><a href="https://travis-ci.com/MeadowSuite/Meadow"><img src="https://travis-ci.com/MeadowSuite/Meadow.svg?branch=master"></a></td>
  </tr>
</table>

## Quick start

Install [.NET Core SDK v2.1.4 or higher](https://www.microsoft.com/net/download), then run these commands in a new directory for your project:

```bash
dotnet new -i Meadow.ProjectTemplate
dotnet new meadow
```

Open your project directory in VSCode or your favorite C# IDE.

## Guides

* [Writing unit tests](https://github.com/MeadowSuite/Meadow/wiki/Getting-Started-with-Unit-Tests) - getting started writing tests against Solidity contracts and generating code coverage reports.

* [Using the CLI](https://github.com/MeadowSuite/Meadow/wiki/Using-the-CLI) - contract deployment and interaction against testnode or production.

* [VSCode Solidity Debugger](https://github.com/MeadowSuite/Meadow/wiki/Using-the-VSCode-Solidity-Debugger)

* [Solidity Coverage Report](https://github.com/MeadowSuite/Meadow/wiki/Coverage-Report)

* [Configuration](https://github.com/MeadowSuite/Meadow/wiki/Configuration) - Specifying gas defaults, solc version, solc optimizer, accounts, RPC host, unit test parallelism, etc..

* [Usage examples; miscellaneous](https://github.com/MeadowSuite/Meadow/wiki/Usage-Examples) - ABI & RLP encoding, ECSign / ECRecover, testing reverts, etc..
---

## Powerful Solidity contract development, deployment, and interaction

<img src="/images/screenshot1.png?raw=true" width="700" />

Provides an intuitive framework for writing C# to perform contract deployments, transactions, function calls, RPC requests, and more. Solidity source files are automatically compiled and exposed as C# classes with all contract methods, events, and natspec documentation. Includes a personal Ethereum test node that automatically is setup during test executions.

#### Visibility into Solidity revert / exception call stacks

<img src="/images/screenshot2.png?raw=true" width="700" />

Better understanding and investigation of Solidity execution problems. 

---

# Solidity Coverage Reports

<img src="/images/screenshot4.png?raw=true" width="600" />

Perform thorough testing of Solidity codebases. Generate HTML and JSON code coverage reports showing .sol source code coverage for line, branch, and function execution. 

---

# Solidity Debugger

[![vs marketplace](https://img.shields.io/vscode-marketplace/v/hosho.solidity-debugger.svg)](https://marketplace.visualstudio.com/items?itemName=hosho.solidity-debugger)

<img src="/images/screenshot3.png?raw=true" width="800" />

Solidity debugger extension for Visual Studio Code supporting breakpoints, stepping, rewinding, call stacks, local & state variable inspection.

---

# Components

<table>
  <thead>
    <tr>
      <th>Library</th>
      <th>Description</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.EVM">Meadow.EVM</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.EVM"><img src="https://img.shields.io/nuget/v/Meadow.EVM.svg?colorB=blue"/></a>
      </td>
      <td>An Ethereum Virtual Machine that includes: <ul><li>Instructions/opcodes, calling conventions/messages/return values, memory/stack, logs/events, gas, charges/limits, precompiles, contract creation logic. <li>Core Ethereum components: account storage, transaction receipts, transaction pool, blocks, world state, snapshoting/reverting, chain, mining/consensus mechanism/scoring/difficulty/uncles. <li>Underlying dependencies: configuration/genesis block/fork/versioning/chain ID support, modified Merkle Patricia Trees, bloom filters, elliptic curve signing + public key recovery, Ethash, in-memory storage database.</ul></td>
    </tr>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.TestNode">Meadow.TestNode</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.TestNode"><img src="https://img.shields.io/nuget/v/Meadow.TestNode.svg?colorB=blue"></a>
      </td>
      <td>Ethereum "personal blockchain" / "test node" / "RPC Server" / "Ethereum client". Ran as either a standalone server or via programmatic setup / teardown during unit test execution. Supports several non-standard RPC methods for debugging, testing, and coverage report generation.</td>
    </tr>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.TestNode.Host">Meadow.TestNode.Host</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.TestNode.Host"><img src="https://img.shields.io/nuget/v/Meadow.TestNode.Host.svg?colorB=blue"></a>
      </td>
      <td>Standalone test RPC node/server used as a command line tool.</td>
    </tr>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.SolCodeGen">Meadow.SolCodeGen</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.SolCodeGen"><img src="https://img.shields.io/nuget/v/Meadow.SolCodeGen.svg?colorB=blue"></a>
      </td>
      <td>Tool that compiles Solidity source files and generates a C# class for each contract. All public methods and events in the contract ABI are translated to corresponding idiomatic C# methods and event log classes. Solidity NatSpec comments / docs are also translated to IntelliSense / code-completion tooltips. This nuget package can be simply added to a project and Solidity files in the project <code>contracts</code> directory are automatically compiled.</td>
    </tr>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.CoverageReport">Meadow.CoverageReport</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.CoverageReport"><img src="https://img.shields.io/nuget/v/Meadow.CoverageReport.svg?colorB=blue"></a>
      </td>
      <td>Generates HTML and JSON code coverage reports for Solidity source files. Uses execution trace data from the EVM.</td>
    </tr>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.UnitTestTemplate">Meadow.UnitTestTemplate</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.UnitTestTemplate"><img src="https://img.shields.io/nuget/v/Meadow.UnitTestTemplate.svg?colorB=blue"></a>
      </td>
      <td>Test harness providing seamless integration between MSTest and Solidity contracts. Provides a simple workflow where Solidity source files are dropped into a unit test project and C# contract code is automatically generated. C# unit tests can easily deploy/call/transact with contracts. RPC test node servers & clients are automatically boostrapped and provided to unit tests. Code coverage reports are automatically generated after unit tests are ran.</td>
    </tr>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.Cli">Meadow.Cli</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.Cli"><img src="https://img.shields.io/nuget/v/Meadow.Cli.svg?colorB=blue"></a>
      </td>
      <td>Tool that allows contract deployments and interaction through the command line. Solidity source files are live-compiled using a file system watcher. Can be ran against a automatically bootstrapped test RPC node or an externally configured node. Leverages PowerShell Core to a provide cross platform REPL-like environment with powerful tab-completion when interacting with contracts.</td>
    </tr>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.Core">Meadow.Core</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.Core"><img src="https://img.shields.io/nuget/v/Meadow.Core.svg?colorB=blue"></a>
      </td>
      <td><ul><li>RLP and ABI encoding & decoding utils. <li>Implementations of Ethereum / solidity types such as Address, UInt256, Hash, etc. <li>BIP32, BIP39, BIP44, HD account derivation implementation. <li>Fast managed Keccak hashing. <li> ECDSA / secp256k1 utils.</ul></td>
    </tr>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.JsonRpc">Meadow.JsonRpc</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.JsonRpc"><img src="https://img.shields.io/nuget/v/Meadow.JsonRpc.svg?colorB=blue"></a>
      </td>
      <td><ul><li>.NET types for the Ethereum JSON-RPC request & response data structures. <li>.NET interface for all RPC methods. <li>Serialization for JSON/hex object formats <-> Solidity/.NET types.</ul></td>
    </tr>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.JsonRpc.Client">Meadow.JsonRpc.Client</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.JsonRpc.Client"><img src="https://img.shields.io/nuget/v/Meadow.JsonRpc.Client.svg?colorB=blue"></a>
      </td>
      <td>JSON-RPC client implementation, supported transports: http, WebSocket, and IPC.</td>
    </tr>
    <tr>
      <td valign="top">
        <a href="/src/Meadow.JsonRpc.Server">Meadow.JsonRpc.Server</a><br><br>
        <a href="https://www.nuget.org/packages/Meadow.JsonRpc.Server"><img src="https://img.shields.io/nuget/v/Meadow.JsonRpc.Server.svg?colorB=blue"></a>
      </td>
      <td>Fast and lightweight HTTP and WebSockets JSON-RPC server - using Kestrel and managed sockets.</td>
    </tr>
  </tbody>
</table>
  
