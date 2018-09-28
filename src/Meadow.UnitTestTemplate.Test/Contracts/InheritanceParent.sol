pragma solidity ^0.4.21;


contract InheritanceParent {

	function ensureThing(uint _num) internal returns(uint) {

		// quarum must be at least 1 (used to determine existence)
		require(_num > 0);

		_num++;
		return _num;
	}

}