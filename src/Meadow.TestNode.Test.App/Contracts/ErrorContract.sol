pragma solidity ^0.5.0;

contract ErrorContract 
{
    /// @notice Our default contructor
    constructor() public 
	{

    }

	/// @notice Raises a simple error directly in this call.
	function testSimpleError() public
	{
		assert(false);
	}

	function doAssert() public
	{
		assert(false);
	}

	function doRevert() public
	{
		require(false, "some require message");
	}

	function doThrow() public {
		revert();
	}

	/// @notice Raises an error indirectly through another call.
	function testIndirectError() public
	{
		// Call another function to generate the error.
		testSimpleError();
	}

    /// @notice The fallback function
    function() external 
	{

    }

}