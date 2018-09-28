using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SolCodeGen.TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {

            //TestSolCompile();
            //await TestAbi();
            //await Rpc();
            //GenerateSomeZeppelinCode();

            await Task.Yield();
        }

        /*
        static void TestSolCompile()
        {
            var outputType = new[] { OutputType.Abi, OutputType.EvmBytecodeObject, OutputType.DevDoc, OutputType.UserDoc };
            outputType = OutputTypes.All;
            var compiledExample = new SolcLib("TestData").Compile(new[] { "SampleCrowdsale.sol", "IndividuallyCappedCrowdsale.sol", "Ownable.sol", "Crowdsale.sol" }, outputType);
            var contract = compiledExample.Contracts.Values.First().Values.First();
            var bytecode = contract.Evm.Bytecode.Object;
            var abi = contract.Abi;
            var abiJson = JsonConvert.SerializeObject(abi, Formatting.None);
        }*/

        /*
        static void GenerateSomeZeppelinCode()
        {
            const string OPEN_ZEP_DIR = "OpenZeppelin";
            var solcLib = SolcLib.Create(OPEN_ZEP_DIR);
            var srcs = new[] {
                "contracts/AddressUtils.sol",
                "contracts/Bounty.sol",
                "contracts/DayLimit.sol",
                "contracts/ECRecovery.sol",
                "contracts/LimitBalance.sol",
                "contracts/MerkleProof.sol",
                "contracts/ReentrancyGuard.sol",
                "contracts/crowdsale/Crowdsale.sol",
                "contracts/examples/SampleCrowdsale.sol",
                "contracts/token/ERC20/StandardBurnableToken.sol",
            };
            var outputFlags = OutputType.Abi | OutputType.EvmBytecodeObject | OutputType.DevDoc | OutputType.UserDoc;
            var solcOutput = solcLib.Compile(srcs, outputFlags);

            var projDirectory = Directory.GetParent(typeof(Program).Assembly.Location);
            bool projDirFound = false;
            for (var i = 0; i < 5; i++)
            {
                if (projDirectory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
                {
                    projDirFound = true;
                    break;
                }
                projDirectory = projDirectory.Parent;
            }

            if (!projDirFound)
            {
                throw new Exception("Could not find project directory");
            }

            var generatedContractsDir = Path.Combine(projDirectory.FullName, "GeneratedContracts");
            if (Directory.Exists(generatedContractsDir))
            {
                Directory.Delete(generatedContractsDir, true);
            }
            Directory.CreateDirectory(generatedContractsDir);


            foreach (var entry in solcOutput.Contracts.SelectMany(c => c.Value))
            {
                var generator = new ContractGenerator(entry.Key, entry.Value);
                var generatedContractCode = generator.GenerateCodeFile();

                File.WriteAllText(Path.Combine(generatedContractsDir, entry.Key + ".cs"), generatedContractCode);
            }

        }
        */

        /*
        static async Task Rpc()
        {
            Uri server = new Uri("http://127.0.0.1:7545");
            var rpcClient = JsonRpcClient.Create(server);
            var ver = await rpcClient.Version();
            var protoVer = await rpcClient.ProtocolVersion();

            var accounts = await rpcClient.Accounts();
            var balance = await rpcClient.GetBalance(accounts[0], BlockParameterType.Latest);

            await rpcClient.Mine();
            var blockNum = await rpcClient.BlockNumber();
            var block = await rpcClient.GetBlockByNumber(true, blockNum);
        }
        */

        /*
        static async Task TestAbi()
        {
            var outputType = new[] { OutputType.Abi, OutputType.EvmBytecodeObject, OutputType.DevDoc, OutputType.UserDoc };
            //outputType = OutputTypes.All;
            var compiledExample = new SolcLib("TestData").Compile("ExampleContract.sol", outputType);

            var bytecode = compiledExample.Contracts.Values.First().Values.First().Evm.Bytecode.Object;
            var abi = compiledExample.Contracts.Values.First().Values.First().Abi;
            var abiJson = JsonConvert.SerializeObject(abi, Formatting.None);

            ExampleGeneratedContract.ABI_JSON = abiJson;
            ExampleGeneratedContract.BYTECODE_HEX = bytecode;

            Uri server = new Uri("http://127.0.0.1:7545");
            var rpcClient = new JsonRpcClient(server);
            var accounts = await rpcClient.Accounts();
            var exContract = await ExampleGeneratedContract.New(rpcClient, ("test name", true, 22222), new SendParams
            {
                From = accounts[2],
                Gas = 5_000_000,
            }, accounts[3]);


            var echoStringResult = await exContract.echoString("hello world").Call();

            var echoManyResult = await exContract.echoMany(accounts[9], 12345, "sdkfjsdlkfjsdofjdslkfjksdlkfldlskjfklsdjdlkfskldkfldkskfmfklsmkldfmskdlfmklsdmfklsdlmldklfskflksdmfksdklfsldflksdmflksmklfmdkmfdmfklsmdlkfmlsdmfklsdmflksmdlkfsdklfkllfslkdfksmkldfmklsdmklfdlkfmskldmkl").Call();

            var givenNameResult = await exContract.givenName().Call();

            var echoTransaction = await exContract.givenName();

            var getArrStatic = await exContract.getArrayStatic().Call();

            var getArrDynamic = await exContract.getArrayDynamic().Call();

            var echoArrayStatc = await exContract.echoArrayStatic(new uint[] { 123, 0, 99999, 3333333, 16777215 }).Call();

            var echoArrayDynamic = await exContract.echoArrayDynamic(new uint[] { 123, 0, 99999, 3333333, 16777215 }).Call();

            var echoMultipleStatic = await exContract.echoMultipleStatic(1234, true, "0x40515114eEa1497D753659DFF85910F838c6B234").Call();

            var echoMultipleDynamic = await exContract.echoMultipleDynamic("first string", "asdf", "utf8; 4 bytes: 𠾴; 3 bytes: ⏰ works!").Call();

            var boat = await exContract.boat(true, "my string", -11118,
                new Address[] { "0x98E4625b2d7424C403B46366150AB28Df4063408", "0x40515114eEa1497D753659DFF85910F838c6B234", "0xDf0270A6BFf43e7A3Fd92372DfB549292D683D22" },
                99, new ulong[] { 9, 0, ulong.MaxValue }).Call();

            await exContract.noopFunc().Call();

        }
        */

    }
}
