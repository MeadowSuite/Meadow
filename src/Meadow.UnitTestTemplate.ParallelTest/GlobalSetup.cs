using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace Meadow.UnitTestTemplate.ParallelTest
{
    [TestClass]
    public static class GlobalSetup
    {
        [AssemblyInitialize]
        public static async Task Init(TestContext testContext)
        {
            await Task.CompletedTask;
        }

        [AssemblyCleanup]
        public static async Task Cleanup()
        {
            await Global.GenerateCoverageReport();
        }

        public static void Main(string[] args)
        {
            SolidityDebugger.Launch();
        }

    }
}
