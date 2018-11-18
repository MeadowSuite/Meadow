
import "./MultifileInheritableContract.sol";

pragma solidity ^0.5.0;


contract MultifileInheritedContract is MultifileInheritableContract {
 
  constructor() public payable { } 
 
  /**
   * @dev Transfers the current balance to the owner and terminates the contract. 
   */
  function destroy() public onlyOwner {
    address payable ownerPayable = address(uint160(owner));
    selfdestruct(ownerPayable);
  }
 
  function destroyAndSend(address payable _recipient) public onlyOwner {
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

  function testFunctionWithInheritedModifier() public onlyOwner {
	uint test = 7;
	if(test == 8)
		test = 1;
	else if(test == 7)
		test = 2;
	else
		test = 3;
  }

  function testInheritedAssertThrow() public
  {
	testAssertIndirect();
  }
}