pragma solidity ^0.4.24;


contract BitShifting {

  function bitwiseAnd(uint256 a, uint256 b) public pure returns (uint256) {
    return a & b;
  }

  function bitwiseOr(uint256 a, uint256 b) public pure returns (uint256) {
    return a | b;
  }

  function bitwiseXor(uint256 a, uint256 b) public pure returns (uint256) {
    return a ^ b;
  }

  function bitwiseNegation(uint256 a) public pure returns (uint256) {
    return ~a;
  }

  function bitwiseLeftShift(uint256 a, uint256 b) public pure returns (uint256) {
    return a << b;
  }

  function bitwiseRightShift(uint256 a, uint256 b) public pure returns (uint256) {
    return a >> b;
  }

}