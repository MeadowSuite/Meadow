
import "./MultifileInheritableContract.sol";

pragma solidity ^0.4.11;


contract MultifileInheritedContract is MultifileInheritableContract {
 
  function MultifileInheritedContract() payable { } 
 
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

  function testInheritedAssertThrow()
  {
	testAssertIndirect();
  }
}