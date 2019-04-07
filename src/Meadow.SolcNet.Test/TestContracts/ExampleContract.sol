pragma solidity ^0.5.0;

/// @title An example contract title
/// @author Matthew Little
/// @notice This is a test contract
/// @dev Hello dev
contract ExampleContract {

    /// @notice Fake balances variable
    /// @param address auto property getter param
    mapping (address => uint256) public balances;

    /// @notice A test event
    event TestEvent(address indexed _addr, uint64 indexed _id, uint _val);

    event EmptyEvent();

	string public givenName;
	bool public enabledThing;
	uint256 public last;

    /// @notice The constructor
    /// @dev Hi dev
    constructor(string memory _name, bool _enableThing, uint256 _last) public {
		givenName = _name;
		enabledThing = _enableThing;
		last = _last;
    }

	function getArrayStatic() public returns (int16[4] memory) {
		int16[4] memory arr;
		arr[0] = 1;
		arr[1] = -2;
		arr[2] = 29;
		arr[3] = 399;
		return arr;
	}

	function getArrayDynamic() public returns (int16[] memory) {
		int16[] memory arr = new int16[](4);
		arr[0] = 1;
		arr[1] = -2;
		arr[2] = 29;
		arr[3] = 399;
		return arr;
	}

    function echoArrayDynamic(uint24[] memory input) public returns (uint24[] memory) {
        return input;
    }

    function echoArrayStatic(uint24[5] memory input) public returns (uint24[5] memory) {
        return input;
    }

    function echoMultipleStatic(uint32 p1, bool p2, address p3) public returns (uint32 r1, bool r2, address r3) {
        return (p1, p2, p3);
    }

    function echoMultipleDynamic(string memory p1, string memory p2, string memory p3) public returns (string memory r1, string memory r2, string memory r3) {
        return (p1, p2, p3);
    }

    function boat(bool p1, string memory p2, int56 p3, address[] memory p4, uint8 p5, uint64[3] memory p6) public
        returns (bool r1, string memory r2, int56 r3, address[] memory r4, uint8 r5, uint64[3] memory r6) {
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

	function echoString(string memory val) public returns (string memory) {
		return val;
	}

	function echoAddress(address val) public returns (address) {
		return val;
	}

	function echoMany(address addr, uint256 num, string memory str) public returns (address, uint256, string memory) {
		return (addr, num, str);
	}

    function echoInt24(int24 _num) public returns (int24 _result) {
        return _num;
    }

    function noopFunc() public {
        
    }

    /// @notice The fallback function
    function() external {

    }

}