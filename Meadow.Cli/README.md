
## Requirements

* [.NET Core 2.1 SDK](https://www.microsoft.com/net/download)
* [PowerShell Core v6.1.0](https://github.com/PowerShell/PowerShell/releases)


## Install

```console
dotnet tool install -g meadow.cli --add-source https://www.myget.org/F/hosho/
```

## Usage

After installing the dotnet tool globally the command `meadow` should be added to your PATH.
From command line run `meadow`. A Powershell Core session will be launched and loaded with Meadow commands.

```console
$ meadow
PS> 
```

Use `man Some-Command` to show information and available parameters for a command.
Use tab completion for commands and parameters. 
For more information on PowerShell see https://github.com/PowerShell/PowerShell/blob/master/docs/learning-powershell/powershell-beginners-guide.md


#### Generate Accounts

```pwsh
PS> Generate-Accounts
```

Optionally, save account keys to file for later usage. By default they are only in memory and will be gone after existing the CLI.
```pwsh
PS> Write-Accounts -Password *******
```

To load the accounts in a later session:
```pwsh
PS> Read-Accounts -Password *******
```

Accounts are automatically loaded in a later session if saved without encryption.
```pwsh
PS> Write-Accounts -EncryptData $false
```

#### Configuration

Run `man Update-Config` to see all configuration options.

Example for configuring the solc optimization level:

```pwsh
PS> Update-Config -SolcOptimizer 200
```


#### Using an external RPC server

This example uses the Kovan testnet provided by Infura.io's public JSON-RPC over https service.

```pwsh
PS> Update-Config -NetworkHost https://kovan.infura.io:443
```

Another example using a geth node running on the local machine with the default RPC port 8545:
```pwsh
PS> Update-Config -NetworkHost 127.0.0.1:8545
```

Once local accounts and network configuration have been setup, the workspace can be initialized for contract deployment and interaction with the network.

```pwsh
PS> Initialize-Workspace console
# RPC client connected to server https://kovan.infura.io:443. Network version: 42
# Watching for file changes in source directory: Contracts
```

#### Using the built-in test RPC server

Initial the workspace in development mode to spawn a local development / test server:

```pwsh
PS> Initialize-Workspace development
# Starting RPC test server...
# Started RPC test server at http://127.0.0.1:52038
# RPC client connected to server http://127.0.0.1:52038. Network version: 77
# Watching for file changes in source directory: Contracts
```

The server port can be specified with:
```pwsh
PS> Update-Config -NetworkPort 4567
```

#### Contract Deployment

Example of deploying this [EIP20](https://github.com/ConsenSys/Tokens/blob/fdf687c69d998266a95f15216b1955a4965a0a6d/contracts/eip20/EIP20.sol) token contract.

Add the EIP20.sol source files to the `Contracts` directory. The code will be automatically compiled if the workspace is initialized.

```pwsh
PS> $tokenContract = Deploy-Contract EIP20 -_initialAmount 10000 -_tokenName "Test Token" -_decimalUnits 18 -_tokenSymbol "TTO"

PS> $tokenContract = Deploy-Contract EIP20 -_initialAmount 10000 -_tokenName "Test Token" -_decimalUnits 18 -_tokenSymbol "TTO" -FromAccount $accounts[3] -Gas 4000000

PS> $tokenContract.ContractAddress
# 0x007d0cb5e18d8d55503b054f63804e3890d84ac0

PS> $tokenContract = $contracts.EIP20::At($rpcClient, "0x007d0cb5e18d8d55503b054f63804e3890d84ac0", $accounts[0]).Result
```

#### Interacting with Contracts

```pwsh
PS> $tokenContract.decimals().Call().Result
# 18

PS> $tokenContract.balanceOf
# Meadow.Contract.EthFunc[Meadow.Core.EthTypes.UInt256] balanceOf(Meadow.Core.EthTypes.Address _owner)

PS> $tokenContract.balanceOf($accounts[0]).Call().Result
# 10000

PS> $tokenContract.transfer($accounts[3], 15).EventLogs().Result
#    EventName       : Transfer
#    EventSignature  : ddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef
#    Address         : 0x007d0cb5e18d8d55503b054f63804e3890d84ac0
#    BlockHash       : 0x5ed48a10989ae1d24a4afbd17a3422978e145c7af1aa7f6e96d1493644fd8252
#    BlockNumber     : 2906806
#    LogIndex        : 0
#    Data            : {0, 0, 0, 0...}
#    Topics          : {0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef, 0x00000000000000000000000021957f175bea92989d6078d72d39a13cd8418ead,
#                    0x00000000000000000000000040d09902d9df8090ee567649de4b30787e4df1a5}
#    TransactionHash : 0xe5697f0a5572a35077ac33daef0bfbbb2cfe630593c376a4c4f5eddfe29c4de5
#    LogArgs         : {(_from, address, True, 0x21957f175bea92989d6078d72d39a13cd8418ead), (_to, address, True, 0x40d09902d9df8090ee567649de4b30787e4df1a5), (_value, uint256, False, 15)}
#    _from           : 0x21957f175bea92989d6078d72d39a13cd8418ead
#    _to             : 0x40d09902d9df8090ee567649de4b30787e4df1a5
#    _value          : 15

PS> $tokenContract.balanceOf($accounts[3]).Call().Result
# 15

PS> $tokenContract.transfer($accounts[5], 5).EventLogs([Meadow.JsonRpc.Types.TransactionParams]::New($accounts[4])).Result
PS> $tokenContract.transfer($accounts[5], 5).EventLogs((New-Object Meadow.JsonRpc.Types.TransactionParams -Property @{From = $accounts[3]; Gas = 4000000})).Result

```