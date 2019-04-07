using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolcNet.CompileErrors;
using SolcNet.DataDescription.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace SolcNet.Test
{
    [TestClass]
    [Ignore("Pending solc v0.5.0 support for OZ")]
    public class CompileOpenZeppelin
    {
        [TestMethod]
        public void CompileAll()
        {
            var sourceContent = new Dictionary<string, string>();
            var contractFiles = Directory.GetFiles("OpenZeppelin", "*.sol", SearchOption.AllDirectories);
            var solc = new SolcLib();
            var output = solc.Compile(contractFiles, OutputType.EvmDeployedBytecodeSourceMap, errorHandling: CompileErrorHandling.ThrowOnError, soliditySourceFileContent: sourceContent);
        }
    }
}
