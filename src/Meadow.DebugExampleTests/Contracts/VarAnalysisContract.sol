pragma solidity ^0.4.21;

/// @title Variable Analysis Contract (Testing)
/// @author David Pokora
/// @notice This is a contract used to test state/local variable parsing from execution traces.
/// @dev 
contract VarAnalysisContract 
{
    uint public globalVal;
    uint sameVal;
    uint32 un32;
    uint32 un322;
    uint128 un128;
    address globalAddr;
    TestEnum globalEnum;
    bool globalBool1;
    bool globalBool2;
    bytes20 globalAddrBytes;
    bytes globalDynamicBytes;
    string globalString;
    string globalString2;
    uint[] globalArray1;
    uint[2] globalArray2;
    Line globalLine;
    mapping(address => uint) simpleMapping;
    mapping(uint => mapping(uint => TestEnum)) nestedMapping;

    struct Point 
    {
		uint x;
        uint y;
	}
    struct Line 
    {
		Point a;
        Point b;
        uint index;
	}

    /// @notice Our default contructor
    function VarAnalysisContract(uint test) public 
	{
        assert(test == 0);
	}

	enum TestEnum {FIRST,SECOND,THIRD}
    
	function updateStateValues()
	{
        uint x = 777;
        globalVal++;
        sameVal += 2;

        un32 += 3;
        un322 += 6;
        un128 += 12818;
        globalAddr = 0x7070707070707070707070707070707070707070;
        globalEnum = TestEnum.THIRD;
        globalBool1 = true;
        globalBool2 = false;
        globalAddrBytes = bytes20(globalAddr);
        globalDynamicBytes.push(byte(globalDynamicBytes.length));
        globalString = string(globalDynamicBytes);
        globalString2 = "testTestOkayOkay!";
        globalArray1.push(globalArray1.length);
        globalArray2[0] = 1;
        globalArray2[1] = 7;

        simpleMapping[msg.sender] = 77;

        globalLine.a.x += 1;
        globalLine.a.y += 2;
        globalLine.b.x += 3;
        globalLine.b.y += 4;
        globalLine.index += 1;
	}

    function indirectThrowWithLocals(uint param1, uint param2) public returns (address addr1, address addr2)
    {
        return throwWithLocals(param1, param2);
    }
	function throwWithLocals(uint param1, uint param2) public returns (address addr1, address addr2)
	{
        assert(param1 == 778899);
        addr1 = 0x345ca3e014aaf5dca488057592ee47305d9b3e10;
        addr2 = 0x8080808080808080808080808080808080808080;
        int x = -1;
        x *= 2;
        uint y = 0x1080;
        y += 700;
        bool b1 = true;
        b1 = false;
        b1 = true;
        bool b2 = false;
        TestEnum enum1 = TestEnum.FIRST;
        TestEnum enum2 = TestEnum.SECOND;
        TestEnum enum3 = TestEnum.THIRD;
        bytes20 addr1Bytes = bytes20(addr1);
        bytes20 addr2Bytes = bytes20(addr2);
        assert(false);

        // This is never hit, these values should not be reflected when checking locals.
        TestEnum enum4 = TestEnum.FIRST;
        bool b3 = true;
        x = 7;
	}

    function storagePointerGenericVar1(uint[2] storageArray) internal
    {
        var testArray = storageArray;
        testArray[0]++;
        testArray[0]++;
    }
    function storagePointerGenericVar2(uint[2] storage storageArray) internal
    {
        var testArray = storageArray;
        testArray[0]++;
        testArray[0]++;
    }

    function throwWithGenericVars(uint param1, uint param2) public returns (address addr1, address addr2)
	{
        assert(param1 == 778899);
        addr1 = 0x345ca3e014aaf5dca488057592ee47305d9b3e10;
        addr2 = 0x8080808080808080808080808080808080808080;

        var gAddr = addr1; // generic address
        gAddr = 0x8080808080808080808080808080808080808080;
        var gUint16 = 0x111; // generic uint16
        var gUint160 = 0x345ca3e014aaf5dca488057592ee47305d9b3e10; // generic uint160
        var gBytes = new bytes(4); // generic dynamic bytes
        gBytes[0] = 0x77;
        gBytes[1] = 0x88;
        gBytes[2] = 0x99;
        gBytes[3] = 0xAA;

        var gTestEnum = TestEnum.FIRST; // generic enum
        gTestEnum = TestEnum.SECOND;

        storagePointerGenericVar1(globalArray2);
        storagePointerGenericVar2(globalArray2);

        TestEnum[] memory arr3 = new TestEnum[](3);
        arr3[0] = TestEnum.FIRST;
        arr3[1] = TestEnum.SECOND;
        arr3[2] = TestEnum.THIRD;
        TestEnum[] memory arr3_copy = arr3;
        var gTestEnumArray = arr3; // generic enum array
        gTestEnumArray[0] = TestEnum.THIRD;
        gTestEnumArray[1] = TestEnum.THIRD;

        var gBool = true;
        gBool = false;
        gBool = true;

        var gBool2 = gBool;

        var gBytes20 = bytes20(gAddr);

        assert(false);
	}

    function throwBytes(bytes memory param1) public returns (bytes memory result)
	{
        result = new bytes(param1.length + 10);
        uint i = 0;
        for(i = 0; i < param1.length; i++)
        {
            result[i] = param1[i];
        }
        for(; i < result.length; i++)
        {
            result[i] = byte(i - param1.length);
        }

        assert(false);
	}

    function throwArray() public
	{
        uint[2] memory arr1;
        arr1[0] = 0x01;
        arr1[1] = 0x77;
        uint[] memory arr2 = new uint[](7);
        arr2[4] = 4;
        arr2[3] = 3;
        TestEnum[] memory arr3 = new TestEnum[](3);

        // Set our enum values.
        arr3[0] = TestEnum.FIRST;
        arr3[1] = TestEnum.SECOND;
        arr3[2] = TestEnum.THIRD;

        // Create a multidimensional array and set two subarrays.
        uint[][] memory arr4 = new uint[][](8);
        uint[] memory arr4_sub3 = new uint[](3);
        arr4_sub3[1] = 111;
        arr4_sub3[2] = 222;
        uint[] memory arr4_sub7 = new uint[](7);
        arr4_sub7[5] = 555;
        arr4[3] = arr4_sub3;
        arr4[7] = arr4_sub7;
        assert(false);
	}

    function throwStruct() public
    {
        Point memory pointA = Point({x: 77, y: 78});
        Point memory pointB = Point({x: 79, y: 80});
        Line memory line = Line({a: pointA, b: pointB, index: 777});
        assert(false);
    }

    function updateSimpleMapping(address mappingKey, uint mappingValue) public
    {
        simpleMapping[mappingKey] = mappingValue;
    }

    function updateNestedMapping(uint key1, uint key2, TestEnum enumValue) public
    {
        nestedMapping[key1][key2] = enumValue;
    }

    function throwExternalCallDataArgs(address[] testArr, uint256 u1, uint256 u2) external {
        // Arguments in this context (external) should be resolved from calldata, not memory)
        for(uint i=0; i< testArr.length; i+=1) {
            assert(false);
        }
    }

    function throwExternalCallDataArgs2(address[] testArr1, address[] testArr2, uint256 u1, uint256 u2) external {
        // Arguments in this context (external) should be resolved from calldata, not memory)
        for(uint i=0; i< testArr1.length; i+=1) {
            assert(false);
        }
    }

    function throwExternalCallDataArgs3(uint256 u1, address[] testArr1, address[] testArr2, uint256 u2) external {
        // Arguments in this context (external) should be resolved from calldata, not memory)
        for(uint i=0; i< testArr1.length; i+=1) {
            assert(false);
        }
    }
}