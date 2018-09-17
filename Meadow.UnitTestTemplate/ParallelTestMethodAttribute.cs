using ExposedObject;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.UnitTestTemplate
{
    public class ParallelTestMethodAttribute : TestMethodAttribute
    {
        #region Fields
        private readonly TestMethodAttribute _testMethodAttribute;
        #endregion

        #region Constructors
        public ParallelTestMethodAttribute()
        {
        }

        public ParallelTestMethodAttribute(TestMethodAttribute testMethodAttribute)
        {
            _testMethodAttribute = testMethodAttribute;
        }
        #endregion

        #region Functions
        private TestContext GetTestContext(ITestMethod testMethod)
        {
            // Obtain our test method options
            var testMethodOptions = Exposed.From(testMethod).TestMethodOptions;

            // Obtain our test context.
            var testContext = Exposed.From(testMethodOptions).TestContext as TestContext;

            // Return the test context
            return testContext;
        }

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            // Get our test context
            TestContext testContext = GetTestContext(testMethod);

            // Create a new internal test state.
            InternalTestState internalTestState = testContext.ResetInternalTestState();

            // Execute our test method on our main node.
            TestResult mainResult = testMethod.Invoke(Array.Empty<object>());

            // Set a more accurate time elapse duration (end of init to start of cleanup)
            mainResult.Duration = internalTestState.EndTime - internalTestState.StartTime;

            // If we have a parallel node to run tests against..
            if (Global.ExternalNodeTestServices != null)
            {
                // If we are only running against the external node
                if (Global.OnlyUseExternalNode.GetValueOrDefault())
                {
                    // Only using the external node means the above method invocation already ran on
                    // the external node, so we quit with the singular result.
                    return new[] { mainResult };
                }

                // Obtain the display name for our test.
                string testDisplayName = mainResult.DisplayName ?? testMethod.TestMethodName;

                // Reset the internal test state for this test context
                internalTestState = testContext.ResetInternalTestState();

                // Begin using the external node.
                internalTestState.InExternalNodeContext = true;

                // Execute our test method on our parallel node
                TestResult parallelResult = testMethod.Invoke(Array.Empty<object>());

                // Set a more accurate time elapse duration (end of init to start of cleanup)
                parallelResult.Duration = internalTestState.EndTime - internalTestState.StartTime;

                // Stop using the external node.
                internalTestState.InExternalNodeContext = false;

                // Set our test names
                mainResult.DisplayName = $"{testDisplayName} (built-in)";
                parallelResult.DisplayName = $"{testDisplayName} (external)";

                return new[] { mainResult, parallelResult };
            }
            else
            {
                throw new Exception("Failed to run parallel test because parallel test node services failed to initialize properly.");
            }
        }
        #endregion
    }
}
