using Meadow.Contract;
using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Meadow.SolCodeGen.CodeGenerators
{
    class SolcOutputDataResxGenerator
    {
        readonly OutputDescription _solcOutput;
        readonly Dictionary<string, string> _solSourceContent;
        readonly string _solSourceDir;
        readonly string _solidityCompilerVersion;


        public SolcOutputDataResxGenerator(OutputDescription solcOutput, Dictionary<string, string> solSourceContent, string solSourceDir, string solidityCompilerVersion)
        {
            _solcOutput = solcOutput;
            _solSourceContent = solSourceContent;
            _solSourceDir = solSourceDir;
            _solidityCompilerVersion = solidityCompilerVersion;
        }

        public ResxWriter GenerateResx()
        {
            var resxWriter = new ResxWriter();

            resxWriter.AddEntry("SolidityCompilerVersion", _solidityCompilerVersion);

            // Make paths relative
            var solSourceContent = _solSourceContent.ToDictionary(d => Util.GetRelativeFilePath(_solSourceDir, d.Key), d => d.Value);

            // Scan ast json for absolute paths and make them relative
            foreach (var absPathToken in _solcOutput.JObject["sources"].SelectTokens("$..absolutePath").OfType<JValue>())
            {
                var absPath = Util.GetRelativeFilePath(_solSourceDir, absPathToken.Value<string>());
                absPathToken.Value = absPath;
            }

            var solcSourceInfos = new List<SolcSourceInfo>();
            foreach (JProperty item in _solcOutput.JObject["sources"])
            {
                var fileName = Util.GetRelativeFilePath(_solSourceDir, item.Name);
                var id = item.Value.Value<int>("id");
                var astObj = (JObject)item.Value["ast"];
                var sourceContent = solSourceContent[fileName];
                var sourceInfo = new SolcSourceInfo
                {
                    AstJson = astObj,
                    FileName = fileName,
                    ID = id,
                    SourceCode = sourceContent
                };
                solcSourceInfos.Add(sourceInfo);
            }

            var solcSourceInfosJson = JsonConvert.SerializeObject(solcSourceInfos, Formatting.Indented);
            resxWriter.AddEntry("SourcesList", solcSourceInfosJson);

            var solcBytecodeInfos = new List<SolcBytecodeInfo>();

            foreach (JProperty solFile in _solcOutput.JObject["contracts"])
            {
                foreach (JProperty solContract in solFile.Value)
                {
                    var fileName = Util.GetRelativeFilePath(_solSourceDir, solFile.Name);
                    var contractName = solContract.Name;

                    var bytecodeObj = solContract.Value["evm"]["bytecode"];
                    var deployedBytecodeObj = solContract.Value["evm"]["deployedBytecode"];

                    var sourceMap = bytecodeObj.Value<string>("sourceMap");
                    var sourceMapDeployed = deployedBytecodeObj.Value<string>("sourceMap");

                    var opcodes = bytecodeObj.Value<string>("opcodes");
                    var opcodesDeployed = deployedBytecodeObj.Value<string>("opcodes");

                    var bytecode = bytecodeObj.Value<string>("object");
                    var bytecodeDeployed = deployedBytecodeObj.Value<string>("object");

                    var bytecodeHash = KeccakHash.ComputeHash(HexUtil.HexToBytes(bytecode)).ToHexString();
                    var bytecodeDeployedHash = KeccakHash.ComputeHash(HexUtil.HexToBytes(bytecodeDeployed)).ToHexString();

                    solcBytecodeInfos.Add(new SolcBytecodeInfo
                    {
                        FilePath = fileName,
                        ContractName = contractName,
                        SourceMap = sourceMap,
                        Opcodes = opcodes,
                        SourceMapDeployed = sourceMapDeployed,
                        OpcodesDeployed = opcodesDeployed,
                        Bytecode = bytecode,
                        BytecodeDeployed = bytecodeDeployed,
                        BytecodeHash = bytecodeHash,
                        BytecodeDeployedHash = bytecodeDeployedHash
                    });

                }
            }

            var solcBytecodeInfosJson = JsonConvert.SerializeObject(solcBytecodeInfos, Formatting.Indented);
            resxWriter.AddEntry("ByteCodeData", solcBytecodeInfosJson);

            var contractAbis = _solcOutput.ContractsFlattened.ToDictionary(c => c.SolFile + "/" + c.ContractName, c => c.Contract.Abi);
            var contractAbisJson = JsonConvert.SerializeObject(contractAbis, Formatting.Indented);
            resxWriter.AddEntry("ContractAbiJson", contractAbisJson);

            return resxWriter;

        }
    }
}

