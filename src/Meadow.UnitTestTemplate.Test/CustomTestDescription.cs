using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate.Test
{

    [MeadowTestClass]
    public class CustomTestDescriptionWithMeadowTestClassAttribute : ContractTest
    {
        [TestMethod]
        [Description("Custom example description")]
        public async Task ExampleTestWithDescription()
        {
            await Task.CompletedTask;
        }

        [TestMethod]
        [System.ComponentModel.Description("Custom example description")]
        public async Task ExampleTestWithDescription2()
        {
            await Task.CompletedTask;
        }

        [DataTestMethod]
        [Description("thing {0} and thing {1}")]
        [DataRow("some val 1", 32465)]
        [DataRow("next valll", 1111)]
        public async Task ExampleDataDrivenTest(string val1, int num2)
        {
            await Task.CompletedTask;
        }
    }

    [TestClass]
    public class CustomTestDescriptionWithMeadowTestMethodAttributes : ContractTest
    {
        [MeadowTestMethod]
        [Description("Custom example description")]
        public async Task ExampleTestWithDescription()
        {
            await Task.CompletedTask;
        }

        [MeadowDataTestMethod]
        [Description("thing {0} and thing {1}")]
        [DataRow("some val 1", 32465)]
        [DataRow("next valll", 1111)]
        public async Task ExampleDataDrivenTest(string val1, int num2)
        {
            await Task.CompletedTask;
        }
    }
}
