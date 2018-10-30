import "./BasicContract.sol";

pragma solidity ^0.4.24;

contract MainContract {

    constructor() public {
		  BasicContract bc = new BasicContract("test", true, 123);
    }

}