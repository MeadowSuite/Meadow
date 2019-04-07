using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolcNet.NativeLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SolcNet.Test
{

    [TestClass]
    [Ignore("Pending solc v0.5.0 support for OZ")]
    public class EthPMTests
    {
        const string CONTRACT_SRC_DIR = "EthPMContracts";

        [TestMethod]
        public void CompileEthPMContractPath()
        {
            var solcLib = new SolcLib(CONTRACT_SRC_DIR);
            var sourceFiles = Directory.GetFiles(CONTRACT_SRC_DIR, "*.sol", SearchOption.AllDirectories).Select(p => Path.GetRelativePath(CONTRACT_SRC_DIR, p)).ToArray();
            var result = solcLib.Compile(sourceFiles);
        }


    }
}
