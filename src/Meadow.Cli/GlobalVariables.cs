using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Client;
using Meadow.TestNode;
using System;
using System.Management.Automation;

namespace Meadow.Cli
{
    static class GlobalVariables
    {
        public static class VAR_NAMES
        {
            public const string RPC_CLIENT = "rpcClient";
            public const string TEST_NODE_SERVER = "testNodeServer";
            public const string ACCOUNTS = "accounts";
            public const string CONTRACTS = "contracts";
            public const string CONTRACT_TYPES = "contractTypes";
        }


        public static IJsonRpcClient JsonRpcClient { get; set; }
        public static TestNodeServer TestNodeServer { get; set; }
        public static Address[] Accounts { get; set; }
        public static (Address Address, EthereumEcdsa Account)[] AccountKeys { get; set; }
        public static uint? ChainID { get; set; }

        public static Type[] ContractTypes { get; set; }


        public static void SetRpcClient(this SessionState sessionState, IJsonRpcClient rpcClient)
        {
            JsonRpcClient = rpcClient;
            sessionState.PSVariable.Set(new PSVariable(VAR_NAMES.RPC_CLIENT, rpcClient, ScopedItemOptions.AllScope));
        }

        public static void SetTestNodeServer(this SessionState sessionState, TestNodeServer testNodeServer)
        {
            TestNodeServer = testNodeServer;
            sessionState.PSVariable.Set(new PSVariable(VAR_NAMES.TEST_NODE_SERVER, testNodeServer, ScopedItemOptions.AllScope));
        }

        public static void SetAccounts(this SessionState sessionState, Address[] accounts)
        {
            Accounts = accounts;
            sessionState.PSVariable.Set(new PSVariable(VAR_NAMES.ACCOUNTS, accounts, ScopedItemOptions.AllScope));
        }

        public static void SetAccounts(this SessionState sessionState, (Address Address, EthereumEcdsa Account)[] accounts)
        {
            AccountKeys = accounts;
        }

        public static void SetContractTypes(this SessionState sessionState, Type[] contractTypes)
        {
            ContractTypes = contractTypes;
            sessionState.PSVariable.Set(new PSVariable(VAR_NAMES.CONTRACT_TYPES, contractTypes, ScopedItemOptions.AllScope));
        }

        public static bool IsSolCompiled(this PSCmdlet cmdlet)
        {
            return ContractTypes != null;
        }
    }
}
