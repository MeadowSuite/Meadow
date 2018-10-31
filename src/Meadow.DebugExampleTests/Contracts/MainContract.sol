import "./BasicContract.sol";

pragma solidity ^0.4.24;

contract Main {

    constructor() public {
		  BasicContract bc = new BasicContract("test", true, 123);
    }

}