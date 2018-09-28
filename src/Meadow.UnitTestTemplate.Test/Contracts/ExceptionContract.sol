pragma solidity ^0.4.21;

contract ExceptionContract {

	address[] public addressArray;

	constructor(bool _throwOnConstructor) public {
		assert(!_throwOnConstructor);
	}

	function outOfBoundArrayAccess() returns(bool) {
		address someAddr = addressArray[4];
		return true;
	}

	function doRevert() {
		revert("do revert hit");
	}

	function doRequire() {
		bool thingDidWork = false;
		require(thingDidWork, "thing did not work");
	}

	function doAssert() {
		bool thingDidWork = false;
		assert(thingDidWork);
	}

	function doThrow() {
		throw;
	}

	function entryFunction() {
		nextFunction();
	}

	function nextFunction() {
		anotherFunc();
	}

	function anotherFunc() {
		lastFunc();
	}

	function lastFunc() {
		doAssert();
	}



}