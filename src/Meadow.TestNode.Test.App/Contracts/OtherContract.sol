pragma solidity ^0.5.0;


contract OtherContract {

    event DataEvent(uint _int1, uint _int2, uint indexed _int3, string _str, uint indexed _int7, address[] _addrs, bytes _bytes, string _mystr);

    event TestEvent(address indexed _addr, uint64 indexed _id, uint _val, uint _timestamp);


	function emitDataEvent(uint _int1, uint _int2, uint _int3) public {
		address[] memory addrs = new address[](12);
		for (uint i = 0; i < 12; i++) {
			addrs[i] = address(i);
		}
		bytes memory b = new bytes(9);
		b[3] = bytes1(uint8(3));

		emit DataEvent(_int1, _int2, _int3, 
		"All non-indexed arguments will be stored in the data part of the log. All non-indexed arguments will be stored in the data part of the log. All non-indexed arguments will be stored in the data part of the log. All non-indexed arguments will be stored in the data part of the log. All non-indexed arguments will be stored in the data part of the log.",
		7, addrs, b, "another string that should be hashed since its indexed");
	}

}