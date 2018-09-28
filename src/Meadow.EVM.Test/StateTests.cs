using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Data_Types.State;
using Xunit;

namespace Meadow.EVM.Test
{
    public class StateTests
    {
        [Fact]
        public void SimpleStateTests()
        {
            // Create a state fresh.
            State state = new State();

            // Create a state snapshot of the blank state
            StateSnapshot cleanSnapshot = state.Snapshot();
            StateSnapshot modifiedSnapshot = null;

            // Create an account and assert it should be blank.
            Address address = new Address(0);
            Address address2 = new Address(1);

            // This loop is made to 1) test clean state and 2) modified state, test modifications. 3) do the same edits using snapshots and verify those work too, then finally 4) test with committing changes between.
            for (int i = 0; i < 4; i++)
            {
                // 1) VERIFY CLEAN VARIABLES
                Assert.True(state.GetAccount(address).IsBlank); // account is blank at first
                Assert.Equal(0, state.GetBalance(address));
                Assert.Equal(0, state.GetBalance(address2));
                Assert.Empty(state.GetCodeSegment(address)); // empty code segment at first
                Assert.Equal(0, state.GetStorageData(address, 0)); // storage has no values, should all be 0.
                Assert.Equal(0, state.GetStorageData(address2, 0)); // storage has no values, should all be 0.
                Assert.Equal(0, state.GetNonce(address)); // nonce is 0 by default
                Assert.Equal(0, state.GetNonce(address2)); // nonce is 0 by default
                Assert.Equal(0, state.TransactionRefunds);
                Assert.False(state.GetAccount(address).IsDirty);

                // 2) SET SOME VARIABLES
                if (modifiedSnapshot == null)
                {
                    state.SetBalance(address, 100);
                    state.TransferBalance(address, address2, 25); // address1 is now 75, address 2 is 25
                    state.SetCodeSegment(address, new byte[] { 77 });
                    state.SetStorageData(address, 0, 555555);
                    state.SetStorageData(address2, 0, 2);
                    state.SetStorageData(address, 0, 1);
                    state.SetNonce(address2, 777);
                    state.ModifyBalanceDelta(address2, 5); // address2 += 5, now 30
                    state.IncrementNonce(address2);
                    state.AddGasRefund(50);
                    state.AddGasRefund(49);

                    // Create a modified state snapshot.
                    modifiedSnapshot = state.Snapshot();
                }
                else
                {
                    // This commits changes before reverting so it shouldn't have any affect.
                    if (i == 3)
                    {
                        state.CommitChanges();
                    }

                    // Revert to our modified snapshot.
                    state.Revert(modifiedSnapshot);

                    // This pushes all changes to the trie, accounts will no longer be 'dirty' (marked as having uncommitted changes).
                    if (i == 2)
                    {
                        state.CommitChanges();
                    }
                }

                // 3) VERIFY MODIFIED VARIABLES
                Assert.False(state.GetAccount(address).IsBlank);
                Assert.Equal(75, state.GetBalance(address));
                Assert.Equal(30, state.GetBalance(address2));
                Assert.True(state.GetCodeSegment(address).ValuesEqual(new byte[] { 77 }));
                Assert.Equal(1, state.GetStorageData(address, 0)); // storage value should be 1 for first address
                Assert.Equal(2, state.GetStorageData(address2, 0)); // storage value should be 2 for second address
                Assert.Equal(0, state.GetNonce(address)); // nonce is 0 by default
                Assert.Equal(778, state.GetNonce(address2)); // we set this nonce so we verify value
                Assert.Equal(99, state.TransactionRefunds);
                Assert.Equal(i != 2, state.GetAccount(address).IsDirty); // on this iteration the account will have been commited so it's clean, on other iteration it's dirty. (Note: i == 3 looks the same, but it commits after snap shot, and before revert, so it still has dirty state).

                // Reset storage data and verify
                state.ResetStorageData(address);
                Assert.Equal(0, state.GetStorageData(address, 0)); // storage has no values, should all be 0.


                // 4) REVERT STATE TO CLEAN STATE.

                // This commits changes before reverting so it shouldn't have any affect.
                if (i == 3)
                {
                    state.CommitChanges();
                }

                // Revert to our clean snapshot.
                state.Revert(cleanSnapshot);

                // This pushes all changes to the trie, accounts will no longer be 'dirty' (marked as having uncommitted changes).
                if (i == 2)
                {
                    state.CommitChanges();
                }
            }
        }
    }
}
