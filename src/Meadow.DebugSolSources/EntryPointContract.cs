using Meadow.Contract;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.DebugSolSources
{
    class EntryPointContract
    {
        public string ContractPath { get; private set; }
        public string ContractName { get; private set; }
        public Abi[] Abi { get; private set; }

        const string SIMPLE_SETUP_HELP = "Specify an entry point contract in 'launch.json' or create a contract named 'Main' with a parameterless constructor.";

        private EntryPointContract(KeyValuePair<(string FilePath, string ContractName), Abi[]> item)
        {
            ContractPath = item.Key.FilePath;
            ContractName = item.Key.ContractName;
            Abi = item.Value;
        }

        public static EntryPointContract FindEntryPointContract(AppOptions opts, GeneratedSolcData generatedSolcData)
        {

            // If both singleFile and entryPoint are specified..
            if (!string.IsNullOrEmpty(opts.SingleFile) && !string.IsNullOrEmpty(opts.EntryContractName))
            {
                var matchingContracts = generatedSolcData.ContractAbis
                    .Where(c => c.Key.FilePath.Equals(opts.SingleFile, StringComparison.OrdinalIgnoreCase))
                    .Where(c => c.Key.ContractName.Equals(opts.EntryContractName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (matchingContracts.Length == 0)
                {
                    throw new Exception($"No matching contracts found for file {opts.SingleFile} and contract '{opts.EntryContractName}'");
                }
                else if (matchingContracts.Length > 1)
                {
                    throw new Exception($"Multiple matching contracts found for file {opts.SingleFile} and contract '{opts.EntryContractName}'");
                }

                return new EntryPointContract(matchingContracts[0]);
            }

            // If only singleFile is specified..
            else if (!string.IsNullOrEmpty(opts.SingleFile))
            {
                var matchingContracts = generatedSolcData.ContractAbis
                    .Where(c => c.Key.FilePath.Equals(opts.SingleFile, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (matchingContracts.Length == 0)
                {
                    throw new Exception($"No contracts found in ${opts.SingleFile}'");
                }
                else if (matchingContracts.Length > 1)
                {
                    // Found multiple contracts in file, see if one is the default "Main" contract.
                    var mainContract = matchingContracts
                        .Where(c => c.Key.ContractName.Equals("Main", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    // Check if there is a single parameterless constructor contract.
                    if (mainContract.Length == 0)
                    {
                        mainContract = matchingContracts
                            .Where(c => c.Value.Any(a => a.Type == AbiType.Constructor && a.Inputs?.Length == 0))
                            .ToArray();
                    }

                    if (mainContract.Length == 1)
                    {
                        return new EntryPointContract(mainContract[0]);
                    }

                    throw new Exception($"Multiple contracts found in {opts.SingleFile}. {SIMPLE_SETUP_HELP}");
                }
                else
                {
                    return new EntryPointContract(matchingContracts[0]);
                }
            }

            // If only entryPoint is specified..
            else if (!string.IsNullOrEmpty(opts.EntryContractName))
            {
                var matchingContracts = generatedSolcData.ContractAbis
                    .Where(c => c.Key.ContractName.Equals(opts.EntryContractName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (matchingContracts.Length == 0)
                {
                    throw new Exception($"No matching contracts found matching '{opts.EntryContractName}'");
                }
                else if (matchingContracts.Length > 1)
                {
                    throw new Exception($"Multiple contracts found matching '{opts.EntryContractName}'.");
                }
                else
                {
                    return new EntryPointContract(matchingContracts[0]);
                }
            }

            // If neither entryPoint nor singleFile are specified..
            else
            {
                if (generatedSolcData.ContractAbis.Count == 0)
                {
                    throw new Exception($"No contracts founds.");
                }
                else if (generatedSolcData.ContractAbis.Count > 1)
                {
                    throw new Exception($"Multiple contracts found. {SIMPLE_SETUP_HELP}");
                }
                else
                {
                    return new EntryPointContract(generatedSolcData.ContractAbis.First());
                }
            }

        }
    }
}