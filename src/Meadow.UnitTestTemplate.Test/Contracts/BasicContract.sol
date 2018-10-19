pragma solidity ^0.4.25;


/// @title An example contract title
/// @author Matthew Little
/// @notice This is a test contract
/// @dev Hello dev
contract BasicContract {

    /// @notice Fake balances variable
    /// @param address auto property getter param
    mapping (address => uint256) public balances;

    /// @notice A test event
    event TestEvent(address indexed _addr, uint64 indexed _id, uint _val);
    event TestEvent1( uint64 indexed _id, uint _val);
    event TestEvent2( uint64 indexed _id, uint _val);
    event TestEvent3( uint64 indexed _id, uint _val);
    event TestEvent4( uint64 indexed _id, uint _val);
    event TestEvent5( uint64 indexed _id, uint _val);
    event TestEvent6( uint64 indexed _id, uint _val);
    event TestEvent7( uint64 indexed _id, uint _val);
    event TestEvent8( uint64 indexed _id, uint _val);
    event EmptyEvent();

	string public givenName;
	bool public enabledThing;
	uint256 public last;
	uint64 public eventIDCounter;
    uint64 public eventIDCounter1;
	uint public eventValCounter;
    uint public eventValCounter1;
	uint public testValue;

    /// @notice The constructor
    constructor(string _name, bool _enableThing, uint256 _last) public {
		givenName = _name;
		enabledThing = _enableThing;
		last = _last;
    }

    function verifyInt(int x) returns (int)
    {
        assert(x == 778899);
        return x;
    }
    function verifyInt2(int x) returns (int)
    {
        assert(x == -778899);
        return x;
    }
    function verifyInt3(int40 x) returns (int40)
    {
        assert(x == 778899);
        return x;
    }
    function verifyInt4(int40 x) returns (int40)
    {
        assert(x == -778899);
        return x;
    }
    function verifyInt5(int16 x) returns (int16)
    {
        assert(x == 7788);
        return x;
    }
    function verifyInt6(int16 x) returns (int16)
    {
        assert(x == -7788);
        return x;
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

    function noopFunc() public {
        
    }

	function incrementValCounter() public returns(uint) {
		eventValCounter += 2;
		return eventValCounter;
	}

    function incrementValAndId() public returns(bool) {
		eventIDCounter += 1;
        eventValCounter += 2;
        return true;
	}

	function getValCounter() public returns (uint _valCounter) {
		return eventValCounter;
	}

    function emitTheEvents() public {
		incrementValAndId();
        emit TestEvent1(eventIDCounter, eventValCounter);
        incrementValAndId();
        emit TestEvent2(eventIDCounter, eventValCounter);
        incrementValAndId();
        emit TestEvent3(eventIDCounter, eventValCounter);
        incrementValAndId();
        emit TestEvent4(eventIDCounter, eventValCounter);
        incrementValAndId();
        emit TestEvent5(eventIDCounter, eventValCounter);
        incrementValAndId();
        emit TestEvent6(eventIDCounter, eventValCounter);
        incrementValAndId();
        emit TestEvent7(eventIDCounter, eventValCounter);
        incrementValAndId();
        emit TestEvent8(eventIDCounter, eventValCounter);

    }

	function emitTheEvent() public {
		eventIDCounter += 1;
		incrementValCounter();
        emit TestEvent(msg.sender, eventIDCounter, eventValCounter);
    }

	function simpleCall() private returns (uint result) {
		return 3;
    }

    function intTestN1() public returns (int120) {
	    return -1;
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

	// -----------------------------------------------------------------------------------------------------

    /// @notice The fallback function
    function() public {

    }

}