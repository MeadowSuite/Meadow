pragma solidity ^0.5.0;

contract ArrayEncodingTests {
    
    event MultiDimArrayEvent(uint64[1][2][3] _arr);
    function emitMultiDimArrayEvent(uint64[1][2][3] memory input) public {
        emit MultiDimArrayEvent(input);
    }

    function takeUIntArrayStatic(uint[3] memory items) public {
        emit UIntEvent(items[0], items[1]);
    }

    function takeUIntArrayDynamic(uint[] memory items) public {
        emit UIntEvent(items[0], items[1]);
    }

    function takeUIntArrayStatic2(uint[3] memory items1, uint[3] memory items2) public {
        emit UIntEvent(items1[0], items2[1]);
    }

    function takeUIntArrayDynamic2(uint[] memory items1, uint[] memory items2) public {
        emit UIntEvent(items1[0], items2[1]);
    }

    event UIntEventArrayStatic(uint[3] _items1, uint[3] _items2);
    function takeUIntArrayStatic3(uint[3] memory items1, uint[3] memory items2) public {
        emit UIntEventArrayStatic(items1, items2);
    }

    event UIntEventArrayDynamic(uint[] _items1, uint[] _items2);
    function takeUIntArrayDynamic3(uint[] memory items1, uint[] memory items2) public {
        emit UIntEventArrayDynamic(items1, items2);
    }

    event UIntEvent(uint _item1, uint _item2);

    function getUIntArrayStatic(uint[3] memory input1, uint[3] memory input2) public returns (uint[3] memory result1, uint[3] memory result2) {
        return (input1, input2);
    }

    function getUIntArrayStatic() public returns (uint[3] memory result1, uint[3] memory result2) {
        uint[3] memory arr1;
        arr1[0] = 1;
        arr1[1] = 2;
        arr1[2] = 3;

        uint[3] memory arr2;
        arr2[0] = 4;
        arr2[1] = 5;
        arr2[2] = 6;

        return (arr1, arr2);
    }    

    function getUIntArrayDynamic(uint[] memory input1, uint[] memory input2) public returns (uint[] memory result1, uint[] memory result2) {
        return (input1, input2);
    }
    function getUIntArrayDynamic() public returns (uint[] memory result1, uint[] memory result2) {
        uint[] memory arr1;
        arr1.length = 3;
        arr1[0] = 1;
        arr1[1] = 2;
        arr1[2] = 3;

        uint[] memory arr2;
        arr2.length = 3;
        arr2[0] = 4;
        arr2[1] = 5;
        arr2[2] = 6;

        return (arr1, arr2);
    }

    event Bytes32Event(bytes32 _item1, bytes32 _item2);

    function takeBytes32ArrayStatic(bytes32[3] memory items) public {
        emit Bytes32Event(items[0], items[1]);
    }

    function takeBytes32ArrayDynamic(bytes32[] memory items) public {
        emit Bytes32Event(items[0], items[1]);
    }

    function takeBytes32ArrayStatic2(bytes32[3] memory items1, bytes32[3] memory items2) public {
        emit Bytes32Event(items1[0], items2[1]);
    }

    function takeBytes32ArrayDynamic2(bytes32[] memory items1, bytes32[] memory items2) public {
        emit Bytes32Event(items1[0], items2[1]);
    }

    event Bytes32EventArrayStatic(bytes32[3] _items1, bytes32[3] _items2);
    function takeBytes32ArrayStatic3(bytes32[3] memory items1, bytes32[3] memory items2) public {
        emit Bytes32EventArrayStatic(items1, items2);
    }

    event Bytes32EventArrayDynamic(bytes32[] _items1, bytes32[] _items2);
    function takeBytes32ArrayDynamic3(bytes32[] memory items1, bytes32[] memory items2) public {
        emit Bytes32EventArrayDynamic(items1, items2);
    }

    function getBytes32ArrayStatic() public returns (bytes32[3] memory result1, bytes32[3] memory result2) {
        bytes32[3] memory arr1;
        arr1[0] = "item1";
        arr1[1] = "2";
        arr1[2] = "arrggg";

        bytes32[3] memory arr2;
        arr2[0] = "1st";
        arr2[1] = "second";
        arr2[2] = "last";

        return (arr1, arr2);
    }    
    
    function getBytes32ArrayDynamic() public returns (bytes32[] memory result1, bytes32[] memory result2) {
        bytes32[] memory arr1;
        arr1.length = 3;
        arr1[0] = "item1";
        arr1[1] = "2";
        arr1[2] = "arrggg";

        bytes32[] memory arr2;
        arr2.length = 3;
        arr2[0] = "1st";
        arr2[1] = "second";
        arr2[2] = "last";

        return (arr1, arr2);
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
		int16[] memory arr;
        arr.length = 4;
		arr[0] = 1;
		arr[1] = -2;
		arr[2] = 29;
		arr[3] = 399;
		return arr;
	}

    function arrayStaticMultiDim3DEcho(uint[2][6][3] memory input) public returns (uint[2][6][3] memory result) {
        return input;
    }

    function arrayStaticMultiDim3D() public returns (uint[2][6][3] memory result) {
        uint x = 0;
        for (uint i = 0; i < 2; i++)
        {
            for (uint j = 0; j < 6; j++)
            {
                for (uint k = 0; k < 3; k++)
                {
                    result[k][j][i] = x++;
                }
            }
        }
        return result;
    }

    function arrayStaticMultiDim2DEcho(uint[6][3] memory input) public returns (uint[6][3] memory result) {
        return input;
    }

    function arrayStaticMultiDim2D() public returns (uint[6][3] memory result) {
        uint x = 0;
        for (uint j = 0; j < 6; j++)
        {
            for (uint k = 0; k < 3; k++)
            {
                result[k][j] = x++;
            }
        }
        return result;
    }


}