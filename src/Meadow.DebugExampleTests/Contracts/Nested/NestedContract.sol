pragma solidity ^0.4.21;


contract NestedContract {

    uint public number;

    /// @notice The constructor
    constructor() public {
		number = 12;
    }

    function incrementNumber(uint x) returns (uint)
    {
        number += x;
        return number;
    }

}