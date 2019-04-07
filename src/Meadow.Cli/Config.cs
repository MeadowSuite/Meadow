using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Meadow.Cli
{

    public class ConfigPropertyInfo
    {
        public PropertyInfo Property { get; set; }
        public Type PropertyType => Property.PropertyType;
        public object DefaultValue { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }


    public class Config
    {
        public const string CONFIG_FILE_NAME = "meadow.config.xml";

        [DefaultValue(0)]
        [DisplayName("SolcOptimizer")]
        [Description("Enables the solc optimizer setting with the given run number. If set to 0 the optimizier is disabled. By default, the optimizer will optimize the contract for 200 runs. If you want to optimize for initial contract deployment and get the smallest output, set it to 1. If you expect many transactions and don’t care for higher deployment cost and output size, set to a high number.")]
        public uint SolcOptimizer { get; set; }

        [DefaultValue(6_000_000)]
        [DisplayName("DefaultGasLimit")]
        [Description("Default gas limit used for deployments and transactions.")]
        public long DefaultGasLimit { get; set; }

        [DefaultValue(100_000_000_000)]
        [DisplayName("DefaultGasPrice")]
        [Description("Default gas price used for deployments and transactions")]
        public long DefaultGasPrice { get; set; }

        [DefaultValue(100)]
        [DisplayName("AccountCount")]
        [Description("The number of accounts to generate")]
        public int AccountCount { get; set; }

        [DefaultValue(2000)]
        [DisplayName("AccountBalance")]
        [Description("The balance (in ether) that accounts should be initially given.")]
        public long AccountBalance { get; set; }

        [DefaultValue("127.0.0.1")]
        [DisplayName("NetworkHost")]
        [Description("TODO")]
        public string NetworkHost { get; set; }

        [DefaultValue(0)]
        [DisplayName("NetworkPort")]
        [Description("If set to 0 and a local test server is spawned then it will use a random available port. If set to 0 and an external RPC server is used then port must be specified here or with host URI.")]
        public uint NetworkPort { get; set; }

        [DefaultValue("Contracts")]
        [DisplayName("SourceDirectory")]
        [Description("TODO...")]
        public string SourceDirectory { get; set; }

        [DefaultValue(true)]
        [DisplayName("UseLocalAccounts")]
        [Description("True to use local client-side accounts, transactions will be signed locally, required to use public remote nodes. False to use RPC server accounts.")]
        public bool UseLocalAccounts { get; set; }

        [DefaultValue(0)]
        [DisplayName("ChainID")]
        [Description("If using local accounts and the remote RPC server does not support eth_chainId then this must be set. Use 1 for mainnet. See https://github.com/ethereum/EIPs/blob/master/EIPS/eip-155.md#list-of-chain-ids")]
        public uint ChainID { get; set; }

        Config()
        {

        }


        public static readonly ConfigPropertyInfo[] ConfigPropInfo;

        static Config()
        {
            ConfigPropInfo = typeof(Config)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new { Prop = p, DefaultValue = p.GetCustomAttribute<DefaultValueAttribute>() })
                .Where(p => p.DefaultValue != null)
                .Select(p => new ConfigPropertyInfo
                {
                    Property = p.Prop,
                    DefaultValue = Convert.ChangeType(p.DefaultValue.Value, p.Prop.PropertyType, CultureInfo.InvariantCulture),
                    Name = p.Prop.GetCustomAttribute<DisplayNameAttribute>().DisplayName,
                    Description = p.Prop.GetCustomAttribute<DescriptionAttribute>().Description
                })
                .ToArray();
        }

        Configuration ReadConfigFile(string dir)
        {
            var configFilePath = Path.Combine(dir, CONFIG_FILE_NAME);
            var configMap = new ExeConfigurationFileMap { ExeConfigFilename = configFilePath };
            
            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            return config;
        }

        public static bool CheckFileExists(string dir)
        {
            var configFilePath = Path.Combine(dir, CONFIG_FILE_NAME);
            return File.Exists(configFilePath); 
        }

        public void Refresh(string dir)
        {
            var configFile = ReadConfigFile(dir);

            (string Name, string Value)[] configValues = configFile.AppSettings
                .Settings
                .Cast<KeyValueConfigurationElement>()
                .Select(s => (s.Key, s.Value))
                .ToArray();

            foreach (var configProp in ConfigPropInfo)
            {
                var match = configValues.FirstOrDefault(c => c.Name.Equals(configProp.Name, StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrWhiteSpace(match))
                {
                    try
                    {
                        var parsedVal = TypeDescriptor.GetConverter(configProp.PropertyType).ConvertFromInvariantString(match);
                        configProp.Property.SetValue(this, parsedVal);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Could not parse configuration item '{configProp.Name}' of type '{configProp.PropertyType.Name}' from value '{match}'");
                        Console.Error.WriteLine(ex);
                    }
                }

                configProp.Property.SetValue(this, Convert.ChangeType(configProp.DefaultValue, configProp.PropertyType, CultureInfo.InvariantCulture));
            }

        }

        public static Config Read(string dir)
        {
            var configResult = new Config();
            configResult.Refresh(dir);
            return configResult;
        }

        public void Save(string dir)
        {
            var configFile = ReadConfigFile(dir);
            configFile.AppSettings.Settings.Clear();
            foreach (var configProp in ConfigPropInfo)
            {
                var configVal = configProp.Property.GetValue(this);
                var defaultVal = configProp.DefaultValue;
                if (!configVal.Equals(defaultVal))
                {
                    var strVal = TypeDescriptor.GetConverter(configProp.PropertyType).ConvertToInvariantString(configVal);
                    configFile.AppSettings.Settings.Add(configProp.Name, strVal);
                }
            }

            configFile.Save(ConfigurationSaveMode.Modified);
        }

    }
}
