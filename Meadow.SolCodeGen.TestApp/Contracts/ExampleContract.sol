pragma solidity ^0.4.21;

/// @title An example contract title
/// @author Matthew Little
/// @notice This is a test contract
/// @dev Hello dev
contract ExampleContract {

    /// @notice Fake balances variable
    /// @param address auto property getter param
    mapping (address => uint256) public balances;

    /// @notice A test event
    event TestEvent(address indexed _addr, uint64 indexed _id, uint _val, uint _timestamp);


    event DataEvent(uint _int1, uint _int2, uint indexed _int3, string _str, uint indexed _int7, address[] _addrs, bytes _bytes, string indexed _mystr);

    event EmptyEvent();

	string public givenName;
	bool public enabledThing;
	uint256 public last;
	uint64 public eventIDCounter;
	uint public eventValCounter;
	uint public testValue;

    /// @notice The constructor
    constructor(string _name, bool _enableThing, uint256 _last) public {
		givenName = _name;
		enabledThing = _enableThing;
		last = _last;
    }

	function getArrayStatic() public returns (int16[4]) {
		int16[4] arr;
		arr[0] = 1;
		arr[1] = -2;
		arr[2] = 29;
		arr[3] = 399;
		return arr;
	}

	function getArrayDynamic() public returns (int16[]) {
		int16[] arr;
        arr.length = 4;
		arr[0] = 1;
		arr[1] = -2;
		arr[2] = 29;
		arr[3] = 399;
		return arr;
	}

    function echoArrayDynamic(uint24[] input) returns (uint24[] result) {
        return input;
    }

    function echoArrayStatic(uint24[5] input) returns (uint24[5] result) {
        return input;
    }

    function echoMultipleStatic(uint32 p1, bool p2, address p3) public returns (uint32 r1, bool r2, address r3) {
        return (p1, p2, p3);
    }

    function echoMultipleDynamic(string p1, string p2, string p3) public returns (string r1, string r2, string r3) {
        return (p1, p2, p3);
    }

    function boat(bool p1, string p2, int56 p3, address[] p4, uint8 p5, uint64[3] p6) public
        returns (bool r1, string r2, int56 r3, address[] r4, uint8 r5, uint64[3] r6) {
            return (p1, p2, p3, p4, p5, p6);
    }

    /// @author Unknown author
    /// @notice This is a test function
    /// @dev Hi dev
    /// @param _num What number
    /// @return true if _num is 9
    function myFunc(uint256 _num) external pure returns (bool isNine) {
        return _num == 9;
    }

	function echoString(string val) public returns (string) {
		return val;
	}

	function echoAddress(address val) public returns (address) {
		return val;
	}

	function echoMany(address addr, uint256 num, string str) public returns (address, uint256, string) {
		return (addr, num, str);
	}

    function echoInt24(int24 _num) public returns (int24 _result) {
        return _num;
    }

	function unreachableCode() internal {
		testValue = 1234;
	}

    function noopFunc() public {
        bool thing = false;
		int mystr = thing ? 2 : 4;
		require(thing);
    }

	function incrementValCounter() public {
		if (testValue == 134343)
		{
			return;
		}
		eventValCounter += 2;
	}

	function emitTheEvent() public {
		eventIDCounter += 1;
		incrementValCounter();
        emit TestEvent(msg.sender, eventIDCounter, eventValCounter, block.timestamp);
    }

	function emitDataEvent(uint _int1, uint _int2, uint _int3) public {
		address[] memory addrs = new address[](12);
		for (uint i = 0; i < 12; i++) {
			addrs[i] = address(i);
		}
		bytes memory b = new bytes(9);
		b[3] = byte(3);

		emit DataEvent(_int1, _int2, _int3, 
		"All non-indexed arguments will be stored in the data part of the log. All non-indexed arguments will be stored in the data part of the log. All non-indexed arguments will be stored in the data part of the log. All non-indexed arguments will be stored in the data part of the log. All non-indexed arguments will be stored in the data part of the log.",
		7, addrs, b, "another string that should be hashed since its indexed");
	}

	function simpleCall() private returns (uint result) {
		
		return 3;
    }

	// -----------------------------------------------------------------------------------------------------
	// INSTRUCTION/GAS TESTING
	// -----------------------------------------------------------------------------------------------------
	function testInstructions1() public returns (uint256 result) {
		testValue = 0;
		testValue += 2; // 2
		testValue /= 2; // 1
		testValue *= 3; // 3
		testValue -= 2; // 1
		testValue %= 2; // 1
		testValue %= 1; // 0 
		testValue += simpleCall(); // 3

		testValue = testValue ** 2; // 9
		testValue /= 9; // 1
		testValue = testValue ** 4; // 1 (more expensive than the previous exponent cost)
		return testValue;
	}

	function sha256str(string str) public returns (bytes32 result) {
		return sha256(str);
	}

	function ripemd160str(string str) public returns (bytes20 result) {
		return ripemd160(str);
	}

	function testECRecover(bytes32 hash, uint8 v, bytes32 r, bytes32 s) public returns (address from) {
		return ecrecover(hash, v, r, s);
	}

	// -----------------------------------------------------------------------------------------------------
	// JUMP COVERAGE TEST
	// -----------------------------------------------------------------------------------------------------
	function testBranchCoverage() public {
		uint test = 0;
		if(test == 0)
		{
			// This is hit.
			test = 1;
		}
		else
		{
			// This is not hit.
			test = 2;

			if(test == 3)
			{
				// This is not hit.
				test = 3;
			}
			else
			{
				// This is also not hit.
				test = 3;
			}
		}

		uint i;
		for(i = 0; i < 2; i++) 
		{
			if(i == 0)
			{
				// This is hit.
				test = 2;
			}
			else
			{
				// This is also hit (later)
				test = 1;
			}
		}

		// Try a If-ElseIf-Else (middle hit)
		if(test == 0)
		{
			// This is not hit.
			test = 1;
		}
		else if(test == 1)
		{
			// This is hit.
			test = 2;
		}
		else
		{
			// This is not hit.
			test = 1;
		}

		// Try a If-ElseIf-Else (longer, middle hit)
		if(test == 0)
		{
			// This is not hit.
			test = 1;
		}
		else if(test == 1)
		{
			// This is not hit.
			test = 1;
		}
		else if(test == 2)
		{
			// This is hit.
			test = 7;
		}
		else
		{
			// This is not hit.
			test = 1;
		}

		// Ternary statements
		test = test == 7 ? 8 : 7; // First hit
		test = test == 8 ? 7 : 7; // First hit
		test = test == 9 ? 6 : 7; // Second hit

		// Try an if we only enter.
		if(test == 7)
			test = 77;

		// Try an if we never enter.
		if(test == 8)
			test = 88;

		if(test == 9)
			test = 99;
		else
			test = 77;

		if(test == 9) test = 99; else test = 77;
		
		// Try asserts
		assert(true);
		assert(false);
	}

	// -----------------------------------------------------------------------------------------------------

    /// @notice The fallback function
    function() public {

    }

}