pragma solidity ^0.4.21;


contract FailDeploymentContract {


	constructor() public {
		address a = 0x0;
		int num = 4 + 5;
		someFunction();
	}

	function someFunction() {
		failingFunction();
	}

	function failingFunction() {
		revert();
	}

}