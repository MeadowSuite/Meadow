pragma solidity ^0.4.11;

contract MultifileInheritableContract {

  address public owner;

  /**
   * @dev The Ownable constructor sets the original `owner` of the contract to the sender
   * account.
   */
  function MultifileInheritableContract() {
    owner = msg.sender;
  }


  /**
   * @dev Throws if called by any account other than the owner.
   */
  modifier onlyOwner() {
    require(msg.sender == owner);
    _;
  }


  /**
   * @dev Allows the current owner to transfer control of the contract to a newOwner.
   * @param newOwner The address to transfer ownership to.
   */
  function transferOwnership(address newOwner) onlyOwner {
    require(newOwner != address(0));      
    owner = newOwner;
  }

  // Test Callstack through multiple contracts and indirect calls.
  function testThrowAssert() public {
  	assert(false);
  }
  function testAssertIndirect() public {
	testThrowAssert();
  }
}