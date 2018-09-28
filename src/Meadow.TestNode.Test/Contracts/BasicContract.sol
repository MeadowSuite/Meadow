pragma solidity ^0.4.21;

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

    function noopFunc() public {
        
    }

	function incrementValCounter() public {
		eventValCounter += 2;
	}

	function getValCounter() public returns (uint _valCounter) {
		return eventValCounter;
	}

	function emitTheEvent() public {
		eventIDCounter += 1;
		incrementValCounter();
        emit TestEvent(msg.sender, eventIDCounter, eventValCounter);
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

	function testModExp(bytes memory _base, bytes memory _exp, bytes memory _mod) public returns(bytes memory ret) {
		// Source: https://gist.github.com/riordant/226f8882556a5c7981b239e4e5d96918
		assembly {
			let bl := mload(_base)
            let el := mload(_exp)
            let ml := mload(_mod)
            
            // Free memory pointer is always stored at 0x40
            let freemem := mload(0x40)
            
            // arg[0] = base.length @ +0
            mstore(freemem, bl)
            
            // arg[1] = exp.length @ +32
            mstore(add(freemem,32), el)
            
            // arg[2] = mod.length @ +64
            mstore(add(freemem,64), ml)
            
            // arg[3] = base.bits @ + 96
            // Use identity built-in (contract 0x4) as a cheap memcpy
            let success := call(450, 0x4, 0, add(_base,32), bl, add(freemem,96), bl)
            
            // arg[4] = exp.bits @ +96+base.length
            let size := add(96, bl)
            success := call(450, 0x4, 0, add(_exp,32), el, add(freemem,size), el)
            
            // arg[5] = mod.bits @ +96+base.length+exp.length
            size := add(size,el)
            success := call(450, 0x4, 0, add(_mod,32), ml, add(freemem,size), ml)
            
            // Total size of input = 96+base.length+exp.length+mod.length
            size := add(size,ml)
            // Invoke contract 0x5, put return value right after mod.length, @ +96
            success := call(sub(gas, 1350), 0x5, 0, freemem, size, add(96,freemem), ml)
            
            // point to the location of the return value (length, bits)
            ret := add(64,freemem) 
            
            mstore(0x40, add(add(96, freemem),ml)) //deallocate freemem pointer
		}
	}
	// -----------------------------------------------------------------------------------------------------

    /// @notice The fallback function
    function() public {

    }

}