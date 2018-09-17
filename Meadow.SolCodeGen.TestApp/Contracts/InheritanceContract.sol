pragma solidity ^0.4.11;

/**
 * @title InheritableContract
 * @dev The InheritableContract contract has an owner address, and provides basic authorization control
 * functions, this simplifies the implementation of "user permissions".
 * 
 * NOTE: THIS SOURCE FILE WAS DERIVED FROM https://ethereumdev.io/inheritance-in-solidity/
 */
contract InheritableContract {

  address public owner;

  /**
   * @dev The Ownable constructor sets the original `owner` of the contract to the sender
   * account.
   */
  function InheritableContract() {
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

}

contract InheritedContract is InheritableContract {
 
  function InheritedContract() payable { } 
 
  /**
   * @dev Transfers the current balance to the owner and terminates the contract. 
   */
  function destroy() onlyOwner {
    selfdestruct(owner);
  }
 
  function destroyAndSend(address _recipient) onlyOwner {
    selfdestruct(_recipient);
  }

  function testFunction() public {
	uint test = 7;
	if(test == 8)
		test = 1;
	else if(test == 7)
		test = 2;
	else
		test = 3;
  }

  function testFunctionWithInheritedModifier() onlyOwner {
	uint test = 7;
	if(test == 8)
		test = 1;
	else if(test == 7)
		test = 2;
	else
		test = 3;
  }
}