pragma solidity ^0.4.21;

/// @title Error Generating Contract (Testing)
/// @author David Pokora
/// @notice This is a contract used to generate errors to test EVM exception tracing, etc.
/// @dev 
contract ErrorContract 
{
    /// @notice Our default contructor
    constructor() public 
	{

    }

	/// @notice Raises a simple error directly in this call.
	function testSimpleError()
	{
		assert(false);
	}

	/// @notice Raises an error indirectly through another call.
	function testIndirectError()
	{
		// Call another function to generate the error.
		testSimpleError();
	}

    /// @notice The fallback function
    function() public 
	{

    }

}