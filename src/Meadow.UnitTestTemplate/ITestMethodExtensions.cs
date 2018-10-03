using ExposedObject;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Meadow.UnitTestTemplate
{
    static class ITestMethodExtensions
    {
        public static TestContext GetTestContext(this ITestMethod testMethod)
        {
            // Obtain our test method options
            var testMethodOptions = Exposed.From(testMethod).TestMethodOptions;

            // Obtain our test context.
            var testContext = Exposed.From(testMethodOptions).TestContext as TestContext;

            // Return the test context
            return testContext;
        }
    }
}
