pragma solidity ^0.5.0;

contract NestedBranches {

	// Ternary testing variable
	uint test1;
	uint test2;

	StructA[] structArray;

	struct StructA {
		StructB[] structs;
	}

	struct StructB {
		uint[] ints;
	}


	constructor() public {
		structArray.length++;
		StructA storage myStructA = structArray[0];
		myStructA.structs.length++;
		myStructA.structs[0].ints.push(555);
		test1 = 0;
		test2 = 0;
	}

	function checkStructTree(uint _num) public returns(bool) {
		uint[] memory a = new uint[](2);
		a[0] = 1;
		a[1] = 0;

		// This should cover both
		for(int i = 0; i < 2; i++)
			test1 = test1 == 1 ? a[test1] : a[test1];

		// This should cover one branch only.
		test1 = test1 == 1 ? a[test1] : a[test1];

		// This should cover the opposite branch as the previous.
		test1 = test1 == 1 ? a[test1] : a[test1];

		if (structArray[0].structs[0].ints[0] == _num) {
			return true;
		}
		else {
			return false;
		}
	}

	function simpleIfStatement(bool _val) public returns (bool){
		// This should cover both
		for(int i = 0; i < 2; i++)
			test2 = test2 == 1 ? 0 : 1;

		// This should cover one branch only.
		test2 = test2 == 1 ? 0 : 1;

		// This should cover the opposite branch as the previous.
		test2 = test2 == 1 ? 0 : 1;

		if (_val) {
			return true;
		}
		else {
			return false;
		}
	}


}