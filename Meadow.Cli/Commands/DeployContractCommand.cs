using ExposedObject;
using Meadow.Contract;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;
using Microsoft.PowerShell.Commands;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.Loader;
using System.Text;

namespace Meadow.Cli.Commands
{

    [Cmdlet(ApprovedVerbs.Deploy, "Contract")]
    [Alias("deployContract")]
    [OutputType(typeof(BaseContract))]
    public class DeployContractCommand : PSCmdlet, IDynamicParameters
    {
        const string CONTRACT_NAME_PARAM = "ContractName";

        //[Parameter(Mandatory = true, Position = 0)]
        //public string ContractName { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        public Address? FromAccount { get; set; }

        [Parameter(Mandatory = false, Position = 2)]
        public UInt256? Gas { get; set; }

        [Parameter(Mandatory = false, Position = 3)]
        public UInt256? GasPrice { get; set; }

        [Parameter(Mandatory = false, Position = 4)]
        public ulong? Nonce { get; set; }

        [Parameter(Mandatory = false, Position = 5)]
        public Address? DefaultFromAddress { get; set; }

        RuntimeDefinedParameter _contractNameParam;

        RuntimeDefinedParameter[] _deploymentParams;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }

        static (MethodInfo MethodInfo, ParameterInfo[] MethodParams) GetContractDeployMethod(Type contractType)
        {
            var deploymentMethod = contractType.GetMethod("Deploy", BindingFlags.Static | BindingFlags.Public);
            var deploymentParams = deploymentMethod.GetParameters();
            var constructorParams = deploymentParams
                .Where(p => !typeof(IJsonRpcClient).IsAssignableFrom(p.ParameterType))
                .Where(p => typeof(TransactionParams) != p.ParameterType)
                .Where(p => typeof(Address?) != p.ParameterType)
                .ToArray();
            return (deploymentMethod, constructorParams);
        }

        protected override void ProcessRecord()
        {
            var contractName = (string)_contractNameParam.Value;
            var contractType = GlobalVariables.ContractTypes.Single(t => t.Name == contractName);
            var (deploymentMethod, constructorParams) = GetContractDeployMethod(contractType);

            var deploymentArgs = new List<object>();

            if (_deploymentParams != null)
            {
                foreach (var runtimeDeploymentArg in _deploymentParams)
                {
                    deploymentArgs.Add(runtimeDeploymentArg.Value);
                }
            }
            else
            {
                foreach (var arg in constructorParams)
                {
                    var paramVal = this.Prompt(arg.ParameterType, arg.Name);
                    deploymentArgs.Add(paramVal);
                }
            }

            deploymentArgs.Add(GlobalVariables.JsonRpcClient);
            deploymentArgs.Add(new TransactionParams
            {
                From = FromAccount,
                Gas = Gas,
                GasPrice = GasPrice,
                Nonce = Nonce
            });
            deploymentArgs.Add(DefaultFromAddress ?? GlobalVariables.Accounts.First());

            dynamic deployContractTask = deploymentMethod.Invoke(null, deploymentArgs.ToArray());
            var contractInstance = deployContractTask.GetAwaiter().GetResult();

            WriteObject(contractInstance);
        }

        protected override void EndProcessing()
        {
            // TODO: After deploying contract, use Set-Variable so user does not have to assign result into variable
            //       Should we try to detect if the output was piped or set to a variable, or always assign output to variable?
            base.EndProcessing();
        }
   
        public object GetDynamicParameters()
        {
            if (!this.IsSolCompiled())
            {
                this.Execute<CompileSolidityCommand>();
            }

            var contractTypeNames = GlobalVariables.ContractTypes.Select(t => t.Name).ToArray();

            _contractNameParam = this.AddRuntimeParamWithValidateSet<string>(
                CONTRACT_NAME_PARAM,
                contractTypeNames,
                new ParameterAttribute { Mandatory = true, Position = 0 });

            try
            {
                string selectedContract = this.GetUnboundValue<string>(CONTRACT_NAME_PARAM, 0);

                var selectedContractType = GlobalVariables.ContractTypes
                    .FirstOrDefault(t => string.Equals(t.Name, selectedContract, StringComparison.InvariantCultureIgnoreCase));

                if (selectedContractType != null)
                {
                    AddRuntimeParamsForContract(selectedContractType);
                }
            }
            catch { }

            return this.GetRuntimeParams();
        }

        void AddRuntimeParamsForContract(Type contractType)
        {
            var (deploymentMethod, constructorParams) = GetContractDeployMethod(contractType);

            var deploymentArgs = new List<object>();

            var runtimeDeploymentParams = new List<RuntimeDefinedParameter>();
            foreach (var arg in constructorParams)
            {
                var deployParam = this.AddRuntimeParam(arg.ParameterType, arg.Name, new ParameterAttribute { Mandatory = true });
                runtimeDeploymentParams.Add(deployParam);
            }

            _deploymentParams = runtimeDeploymentParams.ToArray();
        }



    }
}
