import "./MissingConstructorParent.sol";

pragma solidity ^0.5.0;


contract MissingConstructorChild is MissingConstructorParent {
 
  constructor() public { } 

}