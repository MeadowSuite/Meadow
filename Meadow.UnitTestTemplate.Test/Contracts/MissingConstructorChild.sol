import "./MissingConstructorParent.sol";

pragma solidity ^0.4.11;


contract MissingConstructorChild is MissingConstructorParent {
 
  function MissingConstructorChild() { } 

}