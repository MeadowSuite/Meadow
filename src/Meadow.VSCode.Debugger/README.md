Solidity debugger extension for Visual Studio Code supporting breakpoints, stepping, rewinding, call stacks, local & state variable inspection.

#### Debug a single .sol file containing a single contract

* Ensure no Folders or Workspaces are opened in VSCode and open your `.sol` file.
* The contract must define a parameterless constructor function.
* This constructor function can call other functions in your contract.
* When debug is started the contract is automatically deployed and its constructor function will be ran.

If your contract constructor takes parameters, or if your `.sol` file contains multiple contracts then follow the the next set of instructions below.

#### Debug Solidity codebases with multiple contracts

* Open the folder containing `.sol` files with VSCode.
* Define a contract titled `Main` with a parameterless constructor function.
* This entry point contract can deploy your other contract(s) in its constructor function.
* When debug is started the `Main` contract is automatically deployed and its constructor function will be ran.

#### Debugging contracts with C# unit tests

* [Solidity Debugger quick start guide](https://github.com/MeadowSuite/Meadow/wiki/Using-the-VSCode-Solidity-Debugger)
* [Meadow tool suite](https://github.com/MeadowSuite/Meadow)

<img src="https://github.com/MeadowSuite/Meadow/raw/master/images/screenshot3.png?raw=true" width="800" />


