using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.UnitTestTemplate
{
    internal static class TestContextExtensions
    {
        #region Constants
        private const string TEST_INTERNAL_STATE_KEY = "InternalTestState";
        #endregion

        #region Functions
        internal static InternalTestState GetInternalTestState(this TestContext testContext)
        {
            // Try to obtain our value.
            object value = null;
            bool success = testContext?.Properties?.TryGetValue(TEST_INTERNAL_STATE_KEY, out value) == true;

            // If we could obtain our value, return it.
            if (success)
            {
                return (InternalTestState)value;
            }
            else
            {
                // Create a new internal test state and return that.
                return testContext.ResetInternalTestState();
            }
        }

        internal static void SetInternalTestState(this TestContext testContext, InternalTestState internalTestState)
        {
            // Set our internal test state
            if (testContext?.Properties != null)
            {
                testContext.Properties[TEST_INTERNAL_STATE_KEY] = internalTestState;
            }
        }

        internal static InternalTestState ResetInternalTestState(this TestContext testContext)
        {
            // Create a new internal test state.
            InternalTestState internalTestState = new InternalTestState();

            // Set it in our test context.
            SetInternalTestState(testContext, internalTestState);

            // Return it
            return internalTestState;
        }
        #endregion
    }
}
