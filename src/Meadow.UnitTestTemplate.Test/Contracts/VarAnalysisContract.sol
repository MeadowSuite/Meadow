pragma solidity ^0.4.21;

/// @title Variable Analysis Contract (Testing)
/// @author David Pokora
/// @notice This is a contract used to test state/local variable parsing from execution traces.
/// @dev 
contract VarAnalysisContract 
{
    uint globalVal;
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
    mapping(address => uint) globalMapping1;

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

        globalMapping1[msg.sender] = 77;

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
        uint y = 0x1080;
        bool b1 = true;
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
        arr3[0] = TestEnum.FIRST;
        arr3[1] = TestEnum.SECOND;
        arr3[2] = TestEnum.THIRD;
        uint[][] memory arr4 = new uint[][](8);
        assert(false);
	}

    function throwStruct() public
    {
        Point memory pointA = Point({x: 77, y: 78});
        Point memory pointB = Point({x: 79, y: 80});
        Line memory line = Line({a: pointA, b: pointB, index: 777});
        assert(false);
    }

    function updateMappings(address mappingKey, uint mappingValue) public
    {
        globalMapping1[mappingKey] = mappingValue;
    }
}