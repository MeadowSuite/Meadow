using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SolcNet.DataDescription.Output;
using Meadow.Core.AbiEncoding;
using Meadow.Contract;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;
using System.Collections;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Meadow.Core.Utils;
using Meadow.Core.Cryptography;

namespace Meadow.SolCodeGen.CodeGenerators
{
    class ContractGenerator : GeneratorBase
    {
        readonly string _contractName;
        readonly string _contractSolFileName;
        readonly string _solSourceDir;
        readonly SolcNet.DataDescription.Output.Contract _contract;
        readonly ContractInfo _contractInfo;

        static readonly string JsonRpcClientType = typeof(IJsonRpcClient).FullName;

        static readonly Regex NewLineRegex = new Regex(@"\r\n|\n\r|\n|\r");

        public ContractGenerator(ContractInfo contractInfo, string solSourceDir, string @namespace) : base(@namespace)
        {
            _contractInfo = contractInfo;
            _contractSolFileName = contractInfo.SolFile;
            _contractName = contractInfo.ContractName;
            _contract = contractInfo.ContractOutput;
            _solSourceDir = solSourceDir;
        }

        protected override string GenerateUsingDeclarations()
        {
            var usings = @"
                using Meadow.Contract;
                using Meadow.Core.AbiEncoding;
                using Meadow.Core.EthTypes;
                using Meadow.Core.Utils;
                using Meadow.JsonRpc.Types;
                using SolcNet.DataDescription.Output;
                using System;
                using System.Runtime.InteropServices;
                using System.Threading.Tasks;";

            return usings;
        }

        protected override string GenerateClassDef()
        {
            List<string> eventTypes = new List<string>();
            foreach (var item in _contract.Abi)
            {
                if (item.Type == AbiType.Event)
                {
                    eventTypes.Add($"typeof({_namespace}.{_contractName}.{item.Name})");
                }
            }

            string eventTypesString = string.Empty;
            if (eventTypes.Any()) 
            {
                eventTypesString = ", " + string.Join(", ", eventTypes);
            }

            string bytecodeHash = KeccakHash.ComputeHash(_contract.Evm.Bytecode.ObjectBytes).ToHexString();
            string bytecodeDeployedHash = KeccakHash.ComputeHash(_contract.Evm.DeployedBytecode.ObjectBytes).ToHexString();
            string devDocJson = JsonConvert.SerializeObject(_contract.Devdoc).Replace("\"", "\"\"", StringComparison.Ordinal);
            string userDocJson = JsonConvert.SerializeObject(_contract.Userdoc).Replace("\"", "\"\"", StringComparison.Ordinal);

            string extraSummaryDoc = GetContractSummaryXmlDoc();

            return $@"
                /// <summary>{extraSummaryDoc}</summary>
                [{typeof(SolidityContractAttribute).FullName}(typeof({_contractName}), CONTRACT_SOL_FILE, CONTRACT_NAME, CONTRACT_BYTECODE_HASH, CONTRACT_BYTECODE_DEPLOYED_HASH)]
                public class {_contractName} : {typeof(BaseContract).FullName}
                {{

                    public static Lazy<byte[]> BYTECODE_BYTES = new Lazy<byte[]>(() => {typeof(HexUtil).FullName}.HexToBytes(GeneratedSolcData<{_contractName}>.Default.GetSolcBytecodeInfo(CONTRACT_SOL_FILE, CONTRACT_NAME).Bytecode));

                    public const string CONTRACT_SOL_FILE = ""{_contractSolFileName}"";
                    public const string CONTRACT_NAME = ""{_contractName}"";
                    public const string CONTRACT_BYTECODE_HASH = ""{bytecodeHash}"";
                    public const string CONTRACT_BYTECODE_DEPLOYED_HASH = ""{bytecodeDeployedHash}"";

                    protected override string ContractSolFilePath => CONTRACT_SOL_FILE;
                    protected override string ContractName => CONTRACT_NAME;
                    protected override string ContractBytecodeHash => CONTRACT_BYTECODE_HASH;
                    protected override string ContractBytecodeDeployedHash => CONTRACT_BYTECODE_DEPLOYED_HASH;

                    private {_contractName}({JsonRpcClientType} rpcClient, {typeof(Address).FullName} address, {typeof(Address).FullName} defaultFromAccount)
                        : base(rpcClient, address, defaultFromAccount)
                    {{ 
                        {typeof(EventLogUtil).FullName}.{nameof(EventLogUtil.RegisterDeployedContractEventTypes)}(
                            address.GetHexString(hexPrefix: true)
                            {eventTypesString}
                        );

                    }}

                    public static async Task<{_contractName}> At({JsonRpcClientType} rpcClient, {typeof(Address).FullName} address, {typeof(Address).FullName}? defaultFromAccount = null)
                    {{
                        defaultFromAccount = defaultFromAccount ?? (await rpcClient.Accounts())[0];
                        return new {_contractName}(rpcClient, address, defaultFromAccount.Value);
                    }}

                    {GenerateClassMembers()}
                }}
            ";
        }

        string GenerateClassMembers()
        {
            var template = new StringBuilder();

            // Generate contructor first
            var constructorAbi = _contract.Abi.FirstOrDefault(a => a.Type == AbiType.Constructor);
            template.AppendLine(GenerateConstructor(constructorAbi));

            // Then events
            foreach (var item in _contract.Abi)
            {
                if (item.Type == AbiType.Event)
                {
                    template.AppendLine(GenerateEvent(item));
                }
            }

            // Then functions
            foreach (var item in _contract.Abi)
            {
                if (item.Type == AbiType.Function)
                {
                    template.AppendLine(GenerateFunction(item));
                }
            }

            // Then fallback
            var fallbackAbi = _contract.Abi.FirstOrDefault(a => a.Type == AbiType.Fallback);
            template.AppendLine(GenerateFallbackFunction(fallbackAbi));

            return template.ToString();
        }

        string GenerateConstructor(Abi constructorAbi)
        {
            string inputConstructorArg = string.Empty;
            string inputEncoders = string.Empty;
            bool hasInputs = constructorAbi?.Inputs.Length > 0;
            if (hasInputs)
            {
                var inputs = GenerateInputs(constructorAbi.Inputs);
                inputConstructorArg = GenerateInputString(inputs) + ", ";

                var encoderLines = new string[inputs.Length];

                for (var i = 0; i < inputs.Length; i++)
                {
                    var input = inputs[i];
                    string encoderLine;
                    if (input.AbiType.IsArrayType)
                    {
                        string arrayElementType = GetArrayElementClrTypeName(input.AbiType);
                        string arrayItemEncoder = $"EncoderFactory.LoadEncoder(\"{input.AbiType.ArrayItemInfo.SolidityName}\", default({arrayElementType}))";
                        encoderLine = $"EncoderFactory.LoadEncoder(\"{input.SolidityTypeName}\", {input.Identifier}, {arrayItemEncoder})";
                    }
                    else
                    {
                        encoderLine = $"EncoderFactory.LoadEncoder(\"{input.SolidityTypeName}\", {input.Identifier})";
                    }

                    encoderLines[i] = encoderLine;
                }

                inputEncoders = string.Join(", ", encoderLines);

            }

            var (summaryDoc, paramsDoc) = constructorAbi != null ? GetSummaryXmlDoc(constructorAbi) : (string.Empty, string.Empty);
            string extraSummaryDoc = GetContractSummaryXmlDoc();

            var contractAttrString = $"TypeAttributeCache<{_contractName}, {typeof(SolidityContractAttribute).FullName}>.Attribute";

            string deploymentLine;
            string encodedParamsLine = string.Empty;
            if (constructorAbi?.Inputs?.Length > 0)
            {
                encodedParamsLine = $@"
                    var encodedParams = {typeof(EncoderUtil).FullName}.{nameof(EncoderUtil.Encode)}(
                        {inputEncoders}
                    );";

            }
            else
            {
                encodedParamsLine = "var encodedParams = Array.Empty<byte>();";
            }

            deploymentLine = $"var contractAddr = await ContractFactory.Deploy({contractAttrString}, rpcClient, BYTECODE_BYTES.Value, transactionParams, encodedParams);";

            string xmlDoc = $@"
                    /// <summary>
                    /// Deploys the contract. {extraSummaryDoc} <para/>{summaryDoc}
                    /// </summary>
                    {paramsDoc}
                    /// <param name=""rpcClient"">The RPC client to be used for this contract instance.</param>
                    /// <param name=""defaultFromAccount"">If null then the first account returned by eth_accounts will be used.</param>
                    /// <returns>An contract instance pointed at the deployed contract address.</returns>
            ";

            string methodAttributes = string.Empty;
            if (string.IsNullOrEmpty(_contractInfo.Bytecode))
            {
                methodAttributes = "[Obsolete(\"This contract does not implement all functions and cannot be deployed.\")]";
            }

            return $@"
                    public {_contractName}() {{ }}
                    {xmlDoc}
                    {methodAttributes}
                    public static async Task<{_contractName}> Deploy(
                            {inputConstructorArg}
                            {JsonRpcClientType} rpcClient,
                            {typeof(TransactionParams).FullName} transactionParams = null,
                            {typeof(Address).FullName}? defaultFromAccount = null)
                    {{
                        transactionParams = transactionParams ?? new {typeof(TransactionParams).FullName}();
                        defaultFromAccount = defaultFromAccount ?? transactionParams.From ?? (await rpcClient.Accounts())[0];
                        transactionParams.From = transactionParams.From ?? defaultFromAccount;
                    
                        {encodedParamsLine}
                        {deploymentLine}

                        return new {_contractName}(rpcClient, contractAddr, defaultFromAccount.Value);
                    }}

                    {xmlDoc}
                    {methodAttributes}
                    public static ContractDeployer<{_contractName}> New(
                            {inputConstructorArg}
                            {JsonRpcClientType} rpcClient,
                            {typeof(TransactionParams).FullName} transactionParams = null,
                            {typeof(Address).FullName}? defaultFromAccount = null)
                    {{
                        {encodedParamsLine}

                        return new ContractDeployer<{_contractName}>(rpcClient, BYTECODE_BYTES.Value, transactionParams, defaultFromAccount, encodedParams);
                    }}
            ";
        }

        string GenerateFallbackFunction(Abi methodAbi)
        {
            var (summaryDoc, paramsDoc) = methodAbi != null ? GetSummaryXmlDoc(methodAbi) : (string.Empty, string.Empty);
            string state = methodAbi?.StateMutability.Value ?? string.Empty;

            return $@"
                /// <summary>The contract fallback function. {state}<para/>{summaryDoc}</summary>
                {paramsDoc}
                public EthFunc FallbackFunction => EthFunc.Create(this, Array.Empty<byte>());
            ";
        }

        string GenerateFunction(Abi methodAbi)
        {
            var callDataParams = new List<string>();

            string functionSig = AbiSignature.GetFullSignature(methodAbi);
            callDataParams.Add($"\"{functionSig}\"");

            string inputConstructorArg = string.Empty;
            bool hasInputs = methodAbi.Inputs.Length > 0;
            if (hasInputs)
            {
                var inputs = GenerateInputs(methodAbi.Inputs);
                inputConstructorArg = GenerateInputString(inputs);

                for (var i = 0; i < inputs.Length; i++)
                {
                    var input = inputs[i];
                    string encoderLine;
                    if (input.AbiType.IsArrayType && input.AbiType.ArrayDimensionSizes?.Length > 1)
                    {
                        encoderLine = $"EncoderFactory.LoadEncoderNonGeneric(\"{input.SolidityTypeName}\", {input.Identifier})";
                    }
                    else if (input.AbiType.IsArrayType)
                    {
                        string arrayElementType = GetArrayElementClrTypeName(input.AbiType);
                        string arrayItemEncoder = $"EncoderFactory.LoadEncoder(\"{input.AbiType.ArrayItemInfo.SolidityName}\", default({arrayElementType}))";
                        encoderLine = $"EncoderFactory.LoadEncoder(\"{input.SolidityTypeName}\", {input.Identifier}, {arrayItemEncoder})";
                    }
                    else
                    {
                        encoderLine = $"EncoderFactory.LoadEncoder(\"{input.SolidityTypeName}\", {input.Identifier})";
                    }

                    callDataParams.Add(encoderLine);
                }
            }

            string callDataString = string.Join(", ", callDataParams);

            var outputs = GenerateOutputs(methodAbi.Outputs);
            string outputParams = GenerateOutputString(outputs);
            string outputClrTypes = string.Empty;
            if (outputs.Length > 0)
            {
                outputClrTypes = "<" + string.Join(", ", outputs.Select(s => s.ClrTypeName)) + ">";
            }

            string returnType;
            if (outputs.Length == 0)
            {
                returnType = string.Empty;
            }
            else if (outputs.Length == 1)
            {
                returnType = $"<{outputs[0].ClrTypeName}>";
            }
            else
            {
                returnType = $"<({outputParams})>";
            }

            string[] decoderParams = new string[outputs.Length];
            for (var i = 0; i < outputs.Length; i++)
            {
                string decoder;
                if (outputs[i].AbiType.IsArrayType && outputs[i].AbiType.ArrayDimensionSizes?.Length > 1)
                {
                    var clrType = GetClrTypeName(outputs[i].AbiType, arrayAsEnumerable: false);
                    decoder = $"DecoderFactory.GetDecoder<{clrType}>(\"{outputs[i].AbiType.SolidityName}\")";
                }
                else if (outputs[i].AbiType.IsArrayType)
                {
                    string arrayElementType = GetArrayElementClrTypeName(outputs[i].AbiType);
                    decoder = $"DecoderFactory.GetArrayDecoder(EncoderFactory.LoadEncoder(\"{outputs[i].AbiType.ArrayItemInfo.SolidityName}\", default({arrayElementType})))";
                }
                else
                {
                    decoder = "DecoderFactory.Decode";
                }

                decoderParams[i] = $"\"{methodAbi.Outputs[i].Type}\", {decoder}";
            }

            string decoderStr;
            if (outputs.Length > 0)
            {
                decoderStr = "this, callData, " + string.Join(", ", decoderParams);
            }
            else
            {
                decoderStr = "this, callData";
            }

            var methodName = ReservedKeywords.EscapeIdentifier(methodAbi.Name);

            var (summaryDoc, paramsDoc) = GetSummaryXmlDoc(methodAbi);

            return $@"
                /// <summary>{summaryDoc}</summary>
                {paramsDoc}
                public EthFunc{returnType} {methodName}({inputConstructorArg})
                {{
                    var callData = {typeof(EncoderUtil).FullName}.GetFunctionCallBytes({callDataString});

                    return EthFunc.Create{outputClrTypes}({decoderStr});
                }}
            ";
        }

        string GetContractSummaryXmlDoc()
        {
            string extraSummaryDoc = string.Empty;
            if (!string.IsNullOrEmpty(_contractInfo.ContractOutput.Devdoc.Title))
            {
                extraSummaryDoc += $" <para/>{FormatXmlDocLine(_contractInfo.ContractOutput.Devdoc.Title.Trim())}";
            }

            if (!string.IsNullOrEmpty(_contractInfo.ContractOutput.Devdoc.Details))
            {
                extraSummaryDoc += $" <para/>{FormatXmlDocLine(_contractInfo.ContractOutput.Devdoc.Details.Trim())}";
            }

            if (!string.IsNullOrEmpty(_contractInfo.ContractOutput.Devdoc.Notice))
            {
                extraSummaryDoc += $" <para/>Notice: {FormatXmlDocLine(_contractInfo.ContractOutput.Devdoc.Notice.Trim())}";
            }

            if (!string.IsNullOrEmpty(_contractInfo.ContractOutput.Userdoc.Details))
            {
                extraSummaryDoc += $" <para/>{FormatXmlDocLine(_contractInfo.ContractOutput.Userdoc.Details.Trim())}";
            }

            if (!string.IsNullOrEmpty(_contractInfo.ContractOutput.Userdoc.Title))
            {
                extraSummaryDoc += $" <para/>{FormatXmlDocLine(_contractInfo.ContractOutput.Userdoc.Title.Trim())}";
            }

            if (!string.IsNullOrEmpty(_contractInfo.ContractOutput.Userdoc.Notice))
            {
                extraSummaryDoc += $" <para/>Notice: {FormatXmlDocLine(_contractInfo.ContractOutput.Userdoc.Notice.Trim())}";
            }

            return extraSummaryDoc;
        }

        (string SummaryDoc, string ParamsDoc) GetSummaryXmlDoc(Abi methodAbi)
        {
            string functionSig = AbiSignature.GetFullSignature(methodAbi);

            var methodDevDocNatSpec = _contractInfo.ContractOutput.Devdoc.Methods.TryGetValue(functionSig, out var dDoc) ? dDoc : null;
            var methodUserDocNatSpec = _contractInfo.ContractOutput.Userdoc.Methods.TryGetValue(functionSig, out var uDoc) ? uDoc : null;

            var paramsDevDocNatSpec = methodDevDocNatSpec?.Params ?? new Dictionary<string, string>();
            var paramsUserDocNatSpec = methodUserDocNatSpec?.Params ?? new Dictionary<string, string>();

            var paramDocs = new List<string>();

            if (methodAbi.Inputs?.Length > 0)
            {
                foreach (var inputIdentifier in GenerateInputs(methodAbi.Inputs))
                {
                    string inputNatSpec = paramsDevDocNatSpec.TryGetValue(inputIdentifier.AbiIdentifier, out var docDesc)
                        ? $": {FormatXmlDocLine(docDesc.Trim())}"
                        : string.Empty;

                    if (paramsUserDocNatSpec.TryGetValue(inputIdentifier.AbiIdentifier, out var userDocDesc))
                    {
                        inputNatSpec += $" <para/>{FormatXmlDocLine(userDocDesc.Trim())}";
                    }

                    paramDocs.Add($"/// <param name=\"{inputIdentifier.Identifier}\"><c>{inputIdentifier.SolidityTypeName}</c>{inputNatSpec}</param>");
                }
            }

            string paramsDoc = string.Join(Environment.NewLine, paramDocs);


            string summaryDoc = string.Empty;
            if (methodDevDocNatSpec != null)
            {
                if (!string.IsNullOrEmpty(methodDevDocNatSpec?.Details))
                {
                    summaryDoc += FormatXmlDocLine($"{methodDevDocNatSpec.Details.Trim()}");
                }

                if (!string.IsNullOrEmpty(methodDevDocNatSpec?.Notice))
                {
                    summaryDoc += FormatXmlDocLine($" <para/>Notice: {methodDevDocNatSpec.Notice.Trim()}");
                }
            }

            if (methodUserDocNatSpec != null)
            {
                if (!string.IsNullOrEmpty(methodUserDocNatSpec?.Details))
                {
                    summaryDoc += FormatXmlDocLine($" <para/>{methodUserDocNatSpec.Details.Trim()}");
                }

                if (!string.IsNullOrEmpty(methodUserDocNatSpec?.Notice))
                {
                    summaryDoc += FormatXmlDocLine($" <para/>Notice: {methodUserDocNatSpec.Notice.Trim()}");
                }
            }

            if (methodAbi.Outputs?.Length > 0)
            {
                string returnDesc = string.Empty;
                if (!string.IsNullOrEmpty(methodDevDocNatSpec?.Return))
                {
                    returnDesc = FormatXmlDocLine($" : {methodDevDocNatSpec.Return.Trim()}");
                }

                summaryDoc += $" <para/>Returns <c>{string.Join(",", methodAbi.Outputs.Select(r => r.Type))}</c>{returnDesc}";
                if (!string.IsNullOrEmpty(methodUserDocNatSpec?.Return))
                {
                    summaryDoc += FormatXmlDocLine($" <para/>{methodUserDocNatSpec.Return.Trim()}");
                }
            }

            return (summaryDoc, paramsDoc);
        }

        static string FormatXmlDocLine(string line)
        {
            return NewLineRegex.Replace(line, $" <para/>");
        }

        string GenerateEvent(Abi eventAbi)
        {
            var inputs = GenerateInputs(eventAbi.Inputs);

            string[] propertyLines = new string[eventAbi.Inputs.Length];

            var topicTypes = new List<string>();
            var topicDecoders = new List<string>();

            var dataTypes = new List<string>();
            var dataDecoders = new List<string>();

            var logArgVals = new string[eventAbi.Inputs.Length];

            for (var i = 0; i < eventAbi.Inputs.Length; i++)
            {
                string clrType;
                if (eventAbi.Inputs[i].Indexed.GetValueOrDefault())
                {
                    if (inputs[i].AbiType.IsDynamicType)
                    {
                        clrType = typeof(Hash).FullName;
                        topicTypes.Add("bytes32");
                    }
                    else
                    {
                        clrType = GetClrTypeName(inputs[i].AbiType, arrayAsEnumerable: false);
                        topicTypes.Add(inputs[i].AbiType.SolidityName);
                    }

                    topicDecoders.Add($"DecoderFactory.Decode(topicTypes[{topicTypes.Count - 1}], ref topicBuff, out {inputs[i].Identifier});");
                }
                else
                {
                    clrType = GetClrTypeName(inputs[i].AbiType, arrayAsEnumerable: false);
                    dataTypes.Add(inputs[i].AbiType.SolidityName);
                    string decoder;
                    if (inputs[i].AbiType.IsArrayType && inputs[i].AbiType.ArrayDimensionSizes?.Length > 1)
                    {
                        decoder = $"DecoderFactory.Decode<{clrType}>(dataTypes[{dataTypes.Count - 1}], ref dataBuff, out {inputs[i].Identifier});";
                    }
                    else if (inputs[i].AbiType.IsArrayType)
                    {
                        string arrayElementType = GetArrayElementClrTypeName(inputs[i].AbiType);
                        decoder = $"DecoderFactory.Decode(dataTypes[{dataTypes.Count - 1}], ref dataBuff, out {inputs[i].Identifier}, EncoderFactory.LoadEncoder(\"{inputs[i].AbiType.ArrayItemInfo.SolidityName}\", default({arrayElementType})));";
                    }
                    else
                    {
                        decoder = $"DecoderFactory.Decode(dataTypes[{dataTypes.Count - 1}], ref dataBuff, out {inputs[i].Identifier});";
                    }

                    dataDecoders.Add(decoder);
                }

                propertyLines[i] = $"public readonly {clrType} {inputs[i].Identifier};";

                logArgVals[i] = $"(\"{eventAbi.Inputs[i].Name}\", \"{inputs[i].AbiType.SolidityName}\", {eventAbi.Inputs[i].Indexed.GetValueOrDefault().ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}, {inputs[i].Identifier})";
            }

            string propertyLinesString = string.Join(Environment.NewLine, propertyLines);

            string topicTypesString;
            if (topicTypes.Any()) 
            {
                topicTypesString = "new AbiTypeInfo[] {" + string.Join(", ", topicTypes.Select(t => $"\"{t}\"")) + "}";
            }
            else 
            {
                topicTypesString = "Array.Empty<AbiTypeInfo>()";
            }

            string topicDecodersString = string.Join(Environment.NewLine, topicDecoders);

            string dataTypesString;
            if (dataTypes.Any()) 
            {
                dataTypesString = "new AbiTypeInfo[] {" + string.Join(", ", dataTypes.Select(t => $"\"{t}\"")) + "}";
            }
            else 
            {
                dataTypesString = "Array.Empty<AbiTypeInfo>()";
            }

            string dataDecodersString = string.Join(Environment.NewLine, dataDecoders);

            string logArgValsString;
            if (logArgVals.Any()) 
            {
                logArgValsString = "new(string Name, string Type, bool Indexed, object Value)[]{ " + string.Join("," + Environment.NewLine, logArgVals) + "}";
            }
            else 
            {
                logArgValsString = "Array.Empty<(string Name, string Type, bool Indexed, object Value)>()";
            }

            string eventSignatureHash = AbiSignature.GetSignatureHash(eventAbi);

            var eventName = ReservedKeywords.EscapeIdentifier(eventAbi.Name);

            return $@"
                [{typeof(EventSignatureAttribute).FullName}(SIGNATURE)]
                public class {eventName} : {typeof(EventLog).FullName}
                {{
                    public override string EventName => ""{eventAbi.Name}"";
                    public override string EventSignature => SIGNATURE;

                    public const string SIGNATURE = ""{eventSignatureHash}"";

                    // Event log parameters.
                    {propertyLinesString}

                    public {eventAbi.Name}({typeof(FilterLogObject).FullName} log) : base(log)
                    {{
                        // Decode the log topic args.
                        Span<byte> topicBytes = MemoryMarshal.AsBytes(new Span<{typeof(Meadow.Core.EthTypes.Data).FullName}>(log.Topics).Slice(1));
                        AbiTypeInfo[] topicTypes = {topicTypesString};
                        var topicBuff = new AbiDecodeBuffer(topicBytes, topicTypes);
                        {topicDecodersString}

                        // Decode the log data args.
                        Span<byte> dataBytes = log.Data;
                        AbiTypeInfo[] dataTypes = {dataTypesString};
                        var dataBuff = new AbiDecodeBuffer(dataBytes, dataTypes);
                        {dataDecodersString}
                        
                        // Add all the log args and their metadata to a collection that can be checked at runtime.
                        LogArgs = {logArgValsString};
                    }}
                }}
            ";
        }

        ComponentIdentifier[] GenerateInputs(Input[] inputs)
        {
            ComponentIdentifier[] items = new ComponentIdentifier[inputs.Length];
            int unnamed = 0;
            for (var i = 0; i < items.Length; i++)
            {
                var input = inputs[i];
                var abiType = AbiTypeMap.GetSolidityTypeInfo(input.Type);
                var type = GetClrTypeName(abiType, arrayAsEnumerable: true);
                var identifier = string.IsNullOrEmpty(input.Name) ? $"unamed{unnamed++}" : ReservedKeywords.EscapeIdentifier(input.Name);
                items[i] = new ComponentIdentifier(identifier, input.Name, type, abiType);
            }

            return items;
        }


        public static string GetArrayElementClrTypeName(AbiTypeInfo abiType)
        {
            if (abiType.IsArrayType)
            {
                if (abiType.ArrayItemInfo.ClrType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(abiType.ArrayItemInfo.ClrType))
                {
                    return GetClrTypeName(abiType.ArrayItemInfo, false);
                }
                else
                {
                    return abiType.ArrayItemInfo.ClrTypeName;
                }
            }
            else
            {
                throw new ArgumentException($"Abi type is not an array {abiType}");
            }
        }

        public static string GetClrTypeName(AbiTypeInfo abiType, bool arrayAsEnumerable)
        {
            if (abiType.IsArrayType)
            {
                string arrayItemTypeName;
                if (abiType.IsSpecialBytesType)
                {
                    arrayItemTypeName = typeof(byte).FullName + "[]";
                }
                else if (abiType.ArrayItemInfo.ClrType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(abiType.ArrayItemInfo.ClrType))
                {
                    arrayItemTypeName = GetClrTypeName(abiType.ArrayItemInfo, false);
                }
                else
                {
                    arrayItemTypeName = abiType.ArrayItemInfo.ClrTypeName;
                }

                if (abiType.ArrayDimensionSizes != null && abiType.ArrayDimensionSizes.Length > 1)
                {
                    return arrayItemTypeName + string.Join(string.Empty, Enumerable.Repeat("[]", abiType.ArrayDimensionSizes.Length));
                }
                else if (arrayAsEnumerable)
                {
                    return "System.Collections.Generic.IEnumerable<" + arrayItemTypeName + ">";
                }
                else
                {
                    return arrayItemTypeName + "[]";
                }
            }
            else if (abiType.IsSpecialBytesType)
            {
                if (arrayAsEnumerable)
                {
                    return "System.Collections.Generic.IEnumerable<" + typeof(byte).FullName + ">";
                }
                else
                {
                    return typeof(byte).FullName + "[]";
                }
            }
            else
            {
                return abiType.ClrType.FullName;
            }
        }

        struct ComponentIdentifier
        {
            /// <summary>
            /// Variable name to use in C# code
            /// </summary>
            public string Identifier;

            /// <summary>
            /// Original variable name from Solidity / ABI (can be empty or null)
            /// </summary>
            public string AbiIdentifier;

            public string ClrTypeName;
            public AbiTypeInfo AbiType;
            public string SolidityTypeName => AbiType.SolidityName;

            public ComponentIdentifier(string identifer, string abiIdentifier, string type, AbiTypeInfo abiType)
            {
                Identifier = identifer;
                AbiIdentifier = abiIdentifier;
                ClrTypeName = type;
                AbiType = abiType;
            }
        }

        string GenerateInputString(ComponentIdentifier[] inputs)
        {
            string[] items = new string[inputs.Length];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = inputs[i].ClrTypeName + " " + inputs[i].Identifier;
            }

            return string.Join(", ", items);
        }

        string GenerateInputString(Input[] inputs)
        {
            var p = GenerateInputs(inputs);
            return GenerateInputString(p);
        }

        ComponentIdentifier[] GenerateOutputs(Output[] outputs)
        {
            ComponentIdentifier[] items = new ComponentIdentifier[outputs.Length];
            int unnamed = 0;
            for (var i = 0; i < items.Length; i++)
            {
                var output = outputs[i];
                var abiType = AbiTypeMap.GetSolidityTypeInfo(output.Type);
                var type = GetClrTypeName(abiType, arrayAsEnumerable: false);
                var identifer = string.IsNullOrEmpty(output.Name) ? $"unamed{unnamed++}" : ReservedKeywords.EscapeIdentifier(output.Name);
                items[i] = new ComponentIdentifier(identifer, output.Name, type, abiType);
            }

            return items;
        }


        string GenerateOutputString(ComponentIdentifier[] outputs)
        {
            string[] items = new string[outputs.Length];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = outputs[i].ClrTypeName + " " + outputs[i].Identifier;
            }

            return string.Join(", ", items);
        }

        string GenerateOutputString(Output[] outputs)
        {
            var p = GenerateOutputs(outputs);
            return GenerateOutputString(p);
        }



    }
}
