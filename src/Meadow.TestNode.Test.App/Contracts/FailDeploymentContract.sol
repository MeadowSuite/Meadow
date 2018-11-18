pragma solidity ^0.5.0;


contract FailDeploymentContract {


	constructor() public {
		address a = address(0);
		int num = 4 + 5;
		someFunction();
	}

	function someFunction() public {
		failingFunction();
	}

	function failingFunction() public {
		revert();
	}

}