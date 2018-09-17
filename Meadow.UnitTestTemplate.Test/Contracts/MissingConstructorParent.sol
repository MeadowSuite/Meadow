pragma solidity ^0.4.11;

contract MissingConstructorParent {

  uint public someNum;

  function MissingConstructorParent(uint _someNum) {
    someNum = _someNum;
  }
  
}