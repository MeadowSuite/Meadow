# Update-Config

## SYNOPSIS
This Cmdlet builds a configuration for the test server to use during startup.

## SYNTAX

### Update Config Parameters (Default)
```
Update-Config -Workspace <String> [-SolcOptimizer [<Uint32>]] [-DefaultGasLimit [<Long>]] [-DefaultGasPrice [<Long>]] [-AccountCount [<Int>]] [-AccountBalance [<Long>]] -NetworkHost <String> -NetworkPort <Uint32> [-SourceDirectory [<String>]] [-UseLocalAccounts] [-ChainID [<Uint32>]] [<CommonParameters>]
```

## DESCRIPTION
This Cmdlet builds the configuration for the test server to use during startup.  You can specify several parameters for a custom setup of the RPC test server, or run the command with the minimum parameters to use the default configuration.

## EXAMPLES

### EXAMPLE 1
Basic use
```powershell
C:\PS> Update-Config -Workspace Development -NetworkHost '127.0.0.1' -NetworkPort 8080
```

In this example, the least amount of parameters is specified for a default configuration.  The only required parameters are Workspace, NetworkHost, and NetworkPort.  You will need to decide what workspace type you would like to use and give the server an IP address and port to communicate over.  If you run the Cmdlet like this, the resulting configuration would look liek this:

SolcOptimizer    : 0
						DefaultGasLimit  : 6000000
						DefaultGasPrice  : 100000000000
						AccountCount     : 100
						AccountBalance   : 2000
						NetworkHost      : 127.0.0.1
						NetworkPort      : 90090
						SourceDirectory  : Contracts
						UseLocalAccounts : True
						ChainID          : 0

### EXAMPLE 2
Advanced use
```powershell
C:\PS> Update-Config -Workspace development -SolcOptimizer 0 -DefaultGasLimit 500000 -DefaultGasPrice 100000000 -AccountCount 25 -AccountBalance 1500 -NetworkHost '10.0.0.25' -NetworkPort 90000 -SourceDirectory "$env:USERPROFILE\Documents\Meadow.Cli\TestServer" -UseLocalAccounts $true -ChainID 0
```

This example shows a complete custom configuration for the RPC test server.  The resulting configuration from this example would look like this:

SolcOptimizer    : 0
						DefaultGasLimit  : 500000
						DefaultGasPrice  : 100000000
						AccountCount     : 25
						AccountBalance   : 1500
						NetworkHost      : 10.0.0.25
						NetworkPort      : 90000
						SourceDirectory  : C:\\Users\\mmiller\\Documents\\Meadow.Cli\\TestServer
						UseLocalAccounts : True
						ChainID          : 0

## PARAMETERS

### Workspace
This parameter allows you to specify Console or Development as the intilization workspace for the RPC test server.

```yaml
Type: String
Parameter Sets: Update Config Parameters
Aliases: None

Required: true
Position: named
Default Value: Console
Accepted Values: String
Pipeline Input: False
Dynamic: true
```

### SolcOptimizer
This parameter allows you to specify the use of the Solc compiler optimizer.  The parameter expects a Uint32 as the value.

```yaml
Type: Uint32
Parameter Sets: Update Config Parameters
Aliases: None

Required: false
Position: named
Default Value: $true
Accepted Values: Uint32
Pipeline Input: False
Dynamic: true
```

### DefaultGasLimit
This parameter allows you to specify the default gas limit for the deployment of contracts on the RPC test server.

```yaml
Type: Long
Parameter Sets: Update Config Parameters
Aliases: None

Required: false
Position: named
Default Value: 6000000
Accepted Values: Long
Pipeline Input: False
```

### DefaultGasPrice
This parameter allows you to specify the default gas price for the processing of transactions on the RPC test server.

```yaml
Type: Long
Parameter Sets: Update Config Parameters
Aliases: None

Required: false
Position: named
Default Value: 100000000000
Accepted Values: Long
Pipeline Input: False
```

### AccountCount
This parameter allows you to specify the number of accounts that will reside on the RPC test server.  This parameter should match the value used with the New-Accounts Cmdlet.

```yaml
Type: Int
Parameter Sets: Update Config Parameters
Aliases: None

Required: false
Position: named
Default Value: 100
Accepted Values: Int
Pipeline Input: False
```

### AccountBalance
Thsi parameter allows you to specify the account balance for each of the accounts that reside on the RPC test server for the processing of transactions.

```yaml
Type: Long
Parameter Sets: Update Config Parameters
Aliases: None

Required: false
Position: named
Default Value: 2000
Accepted Values: Long
Pipeline Input: False
```

### NetworkHost
This parameter allows you to specify the IP address of the RPC test server.

```yaml
Type: String
Parameter Sets: Update Config Parameters
Aliases: None

Required: true
Position: named
Default Value: None
Accepted Values: String
Pipeline Input: False
```

### NetworkPort
This parameter allows you to specify the network port that the RPC test server will communicate on.  Please be aware that this cannot supercede a network port already in use by another process.

```yaml
Type: Uint32
Parameter Sets: Update Config Parameters
Aliases: None

Required: true
Position: named
Default Value: None
Accepted Values: Uint32
Pipeline Input: False
```

### SourceDirectory
This parameter allows you to specify the local file path where you would like this configuration data saved.  If a path is not specified, it will use the current directory from your terminal.

```yaml
Type: String
Parameter Sets: Update Config Parameters
Aliases: None

Required: false
Position: named
Default Value: Current Directory
Accepted Values: String
Pipeline Input: False
```

### UseLocalAccounts
This parameter specifies

```yaml
Type: Boolean
Parameter Sets: Update Config Parameters
Aliases: None

Required: false
Position: named
Default Value: $true
Accepted Values: $true
                 $false
Pipeline Input: False
```

### ChainID
This parameter allows you to specify the initial ID for the RPC test server chain.

```yaml
Type: Uint32
Parameter Sets: Update Config Parameters
Aliases: None

Required: false
Position: named
Default Value: 0
Accepted Values: Uint32
Pipeline Input: False
```

### \<CommonParameters\>
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None


## OUTPUTS

### None


## NOTES

### Notes
This Cmdlet is used before the Start-TestServer Cmdlet to configure your instance of the RPC test server.  This allows flexibility in your development environment by letting you chose how you would like the server to run.

## RELATED LINKS

[Meadow Suite site link:](https://meadowsuite.com)

[Hosho Group site link:](https://hosho.io)


*Generated by: PowerShell HelpWriter 2018 v2.2.40*
