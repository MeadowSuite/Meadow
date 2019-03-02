pragma solidity ^0.5.0;

/// @title Error Generating Contract (Testing)
/// @notice This is a contract used to generate errors to test EVM exception tracing, etc.
/// @dev 
contract VarAnalysisContract 
{
	uint globalVal;

    /// @notice Our default contructor
    constructor() public 
	{
	}

	 enum TestEnum {FIRST,SECOND,THIRD}
    
	function updateStateValues()
	{
		uint x = 777;
		globalVal++;
	}

	function throwWithLocals()
	{
		address addr1 = 0x345cA3e014Aaf5dcA488057592ee47305D9B3e10;
		address addr2 = 0x7070707070707070707070707070707070707070;
		int x = -1;
		uint y = 0x1080;
		bool b1 = true;
		bool b2 = false;
		TestEnum enum1 = TestEnum.FIRST;
		TestEnum enum2 = TestEnum.SECOND;
		TestEnum enum3 = TestEnum.THIRD;
		assert(false);

		// This is never hit, these values should not be reflected when checking locals.
		TestEnum enum4 = TestEnum.FIRST;
		bool b3 = true;
		x = 7;
	}
}