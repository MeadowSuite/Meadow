pragma solidity ^0.5.0;

contract MissingConstructorParent {

  uint public someNum;

  constructor(uint _someNum) public {
    someNum = _someNum;
  }
  
}