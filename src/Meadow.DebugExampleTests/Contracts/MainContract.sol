import "./BasicContract.sol";

pragma solidity ^0.5.0;

contract Main {

    constructor() public {
		  BasicContract bc = new BasicContract("test", true, 123);
    }

}