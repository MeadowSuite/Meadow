pragma solidity ^0.5.0;

/// @title An example contract title
/// @notice This is a test contract
/// @dev Hello dev
contract PrecompilesContract {

    /// @notice The constructor
    constructor() public {

    }

	function testSha256(string memory str) public returns (bytes32 result) {
		return sha256(bytes(str));
	}

	function testRipemd160(string memory str) public returns (bytes20 result) {
		return ripemd160(bytes(str));
	}

	function testECRecover(bytes32 hash, uint8 v, bytes32 r, bytes32 s) public returns (address from) {
		return ecrecover(hash, v, r, s);
	}
    function testIdentity(bytes memory input) public returns(bytes memory)
    {
        // Initialize our output array
        bytes memory output = new bytes(input.length);

        assembly 
        {
            // Load our length from our first word at the input pointer (dynamic byte arrays are length (WORD) followed by the data immediately (padded to a size divisible by sizeof(WORD)))
			let length := mload(input)

            // Call our identity precompile (0x04).
            let success := call(450, 0x4, 0, add(input, 32), length, add(output, 32), length)
		}

        return output;
    }
    function testModExp(bytes memory _base, bytes memory _exp, bytes memory _mod) public returns(bytes memory ret) {
		// Source: https://gist.github.com/riordant/226f8882556a5c7981b239e4e5d96918
		assembly 
        {
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
    function() external {

    }

}