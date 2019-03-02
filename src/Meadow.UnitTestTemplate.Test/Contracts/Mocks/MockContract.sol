pragma solidity ^0.5.0;

contract MockContract {

	function echoString(string memory val) public returns (string memory) {
		return val;
	}

}