using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SolcNet.Test
{
    [Ignore]
    [TestClass]
    public class ThreadingTests
    {

        [TestMethod]
        public void ParallelCompiles()
        {
            var taskList = new List<Task>();
            for (var i = 0; i < 1; i++)
            {
                taskList.Add(Task.Run(() =>
                {
                    var solcLib = new SolcLib("OpenZeppelin");
                    for (var j = 0; j < 10; j++)
                    {
                        var srcs = new[] {
                        "contracts/crowdsale/validation/WhitelistedCrowdsale.sol",
                        "contracts/token/ERC20/StandardBurnableToken.sol"
                    };
                        solcLib.Compile(srcs);
                    }
                }));
            }

            Task.WaitAll(taskList.ToArray());
        }

        [TestMethod]
        public void MemLeak()
        {
            var solcLib = new SolcLib("OpenZeppelin");
            for (var j = 0; j < 1000; j++)
            {
                var srcs = new[] {
                        "contracts/crowdsale/validation/WhitelistedCrowdsale.sol",
                        "contracts/token/ERC20/StandardBurnableToken.sol"
                    };
                solcLib.Compile(srcs);
            }
        }




    }
}
