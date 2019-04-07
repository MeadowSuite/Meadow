using JsonDiffPatchDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolcNet.DataDescription.Input;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SolcNet.Test
{
    [TestClass]
    public class CompileOutput
    {

        SolcLib _lib;

        public CompileOutput()
        {
            _lib = new SolcLib();
        }

        [TestMethod]
        public void ExpectedOutput()
        {
            var exampleContract = "TestContracts/ExampleContract.sol";
            var output = _lib.Compile(exampleContract/*, OutputType.Ast | OutputType.LegacyAst*/);
            var originalOutput = JObject.Parse(output.RawJsonOutput).ToString(Formatting.Indented);
            //var expectedOutput = JObject.Parse(File.ReadAllText("TestOutput/ExampleContract.json"));
            var serializedOutput = JsonConvert.SerializeObject(output, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var jdp = new JsonDiffPatch();
            var diffStr = jdp.Diff(originalOutput, serializedOutput);
            if (!string.IsNullOrEmpty(diffStr))
            {
                var diff = JObject.Parse(diffStr).ToString(Formatting.Indented);
            }
        }

        [TestMethod]
        public void SourceMapParsing()
        {
            var exampleContract = "TestContracts/ExampleContract.sol";
            var output = _lib.Compile(exampleContract, OutputType.EvmBytecodeSourceMap);
            var sourceMaps = output.Contracts.Values.First().Values.First().Evm.Bytecode.SourceMap;
            var parsed = sourceMaps.Entries;
            Assert.IsTrue(parsed.Length > 0);
        }

        [TestMethod]
        public void SourceFileContentTracking()
        {
            var exampleContract = "TestContracts/ExampleContract.sol";
            var sourceContent = new Dictionary<string, string>();
            var output = _lib.Compile(exampleContract, OutputType.EvmBytecodeSourceMap, soliditySourceFileContent: sourceContent);
            Assert.AreEqual(sourceContent.First().Key, exampleContract);
        }

        [TestMethod]
        public void OptimizerRuns()
        {
            OutputDescription CompileWithRuns(Optimizer optimizer)
            {
                var exampleContract = "TestContracts/ExampleContract.sol";
                var sourceContent = new Dictionary<string, string>();
                var output = _lib.Compile(exampleContract, OutputType.EvmBytecodeObject, optimizer: optimizer, soliditySourceFileContent: sourceContent);
                return output;
            }

            var runs1 = CompileWithRuns(new Optimizer { Enabled = true, Runs = 1 });
            var sizeRuns1 = runs1.ContractsFlattened[0].Contract.Evm.Bytecode.Object.Length;

            var runs200 = CompileWithRuns(new Optimizer { Enabled = true, Runs = 200 });
            var sizeRuns200 = runs200.ContractsFlattened[0].Contract.Evm.Bytecode.Object.Length;

            var runsDisabled = CompileWithRuns(new Optimizer { Enabled = false });
            var sizeRunsDisabled = runsDisabled.ContractsFlattened[0].Contract.Evm.Bytecode.Object.Length;

            Assert.IsTrue(sizeRunsDisabled > sizeRuns200);
            Assert.IsTrue(sizeRuns1 < sizeRuns200);
        }

    }
}
