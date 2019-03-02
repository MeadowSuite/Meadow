pragma solidity ^0.5.0;


contract NestedContract {

    uint public number;

    /// @notice The constructor
    constructor() public {
		number = 12;
    }

    function incrementNumber(uint x) public returns (uint)
    {
        number += x;
        return number;
    }

}