using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Meadow.UnitTestTemplate
{

    public class MeadowTestMethodAttribute : TestMethodAttribute
    {
        public override TestResult[] Execute(ITestMethod testMethod)
        {
            return TestMethodAttributeHelper.Execute(testMethod);
        }
    }

    public class MeadowDataTestMethodAttribute : DataTestMethodAttribute
    {
        public override TestResult[] Execute(ITestMethod testMethod)
        {
            return TestMethodAttributeHelper.Execute(testMethod);
        }
    }

    class TestMethodAttributeHelper
    {
        public static TestResult[] Execute(ITestMethod testMethod)
        {
            // Get our test context
            TestContext testContext = testMethod.GetTestContext();

            // Get a possible Description attribute
            var desc = testMethod.MethodInfo.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;
            if (string.IsNullOrEmpty(desc))
            {
                desc = testMethod.MethodInfo.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>(inherit: true)?.Description; 
            }

            string customDisplayName = null;

            // Test has method arguments, include them in the description
            if (testMethod.Arguments?.Length > 0)
            {
                // Test has description, use it as a string formatter for the arguments
                if (!string.IsNullOrEmpty(desc))
                {
                    try
                    {
                        customDisplayName = testMethod.TestMethodName + " - " + string.Format(CultureInfo.InvariantCulture, desc, testMethod.Arguments);
                    }
                    catch (Exception ex)
                    {
                        // String format with description failed
                        customDisplayName = testMethod.TestMethodName + " - " + ex.Message;
                    }
                }
                else
                {
                    // No description formatter, only append arguments to test name
                    customDisplayName = testMethod.TestMethodName + " - " + string.Join(", ", testMethod.Arguments);
                }

            }
            // Test only has description, use in display name
            else if (desc != null)
            {
                customDisplayName = testMethod.TestMethodName + " - " + desc;
            }

            if (!string.IsNullOrEmpty(customDisplayName))
            {
                testContext.GetInternalTestState().CustomDisplayName = customDisplayName;
            }

            var executeResult = testMethod.Invoke(testMethod.Arguments);

            if (!string.IsNullOrEmpty(customDisplayName))
            {
                executeResult.DisplayName = customDisplayName;
            }

            return new[] { executeResult };
        }
    }

}
