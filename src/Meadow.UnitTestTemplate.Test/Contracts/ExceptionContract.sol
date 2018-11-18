pragma solidity ^0.5.0;

contract ExceptionContract {

	address[] public addressArray;

	constructor(bool _throwOnConstructor) public {
		assert(!_throwOnConstructor);
	}

	function outOfBoundArrayAccess() public returns(bool) {
		address someAddr = addressArray[4];
		return true;
	}

	function doRevert() public {
		revert("do revert hit");
	}

	function doRequire() public {
		bool thingDidWork = false;
		require(thingDidWork, "thing did not work");
	}

	function doAssert() public {
		bool thingDidWork = false;
		assert(thingDidWork);
	}

	function doThrow() public {
		revert();
	}

	function entryFunction() public {
		nextFunction();
	}

	function nextFunction() public {
		anotherFunc();
	}

	function anotherFunc() public {
		lastFunc();
	}

	function lastFunc() public {
		doAssert();
	}



}