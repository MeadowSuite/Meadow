using Meadow.UnitTestTemplate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.MethodLevel)]

namespace Meadow.DebugExampleTests
{
    [TestClass]
    public static class GlobalSetup
    {
        [AssemblyInitialize]
        public static async Task Init(TestContext testContext)
        {
            Global.HideSolidityFromReport("mocks", "IgnoreContract.sol");
            await Task.CompletedTask;
        }

        [AssemblyCleanup]
        public static async Task Cleanup()
        {
            await Global.Cleanup();
        }

        public static void Main(string[] args)
        {
            SolidityDebugger.Launch();
        }
    }
}
