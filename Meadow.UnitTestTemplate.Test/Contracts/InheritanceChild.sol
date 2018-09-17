pragma solidity ^0.4.21;

import './InheritanceParent.sol';

contract InheritanceChild is InheritanceParent{

	string[] public someStrings;


	function InheritanceChild() public {
		someStrings.push("str1");
	}

	modifier myModifer(uint _num) {
		if (_num == 5) 
			revert();
		_;
	}

	function testThing(uint _num) public myModifer(_num) returns(uint) {
		string storage aString = someStrings[0];
		uint otherNum = super.ensureThing(_num);
		return otherNum;
	}

}