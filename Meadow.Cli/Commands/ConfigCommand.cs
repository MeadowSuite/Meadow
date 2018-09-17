using Meadow.JsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Management.Automation;

namespace Meadow.Cli.Commands
{

    // TODO: Detect config changes and perform updates if needed, examples: gas values, RPC client, RPC server

    [Cmdlet(ApprovedVerbs.Update, "Config")]
    [Alias("config")]
    public class ConfigCommand : PSCmdlet, IDynamicParameters
    {

        [Parameter(Position = 0)]
        [ValidateSet(InitializeWorkspaceCommand.WORKSPACE_CONSOLE, InitializeWorkspaceCommand.WORKSPACE_DEVELOPMENT)]
        public string Workspace { get; set; }

        List<(RuntimeDefinedParameter Param, ConfigPropertyInfo ConfigProp)> _props = new List<(RuntimeDefinedParameter Param, ConfigPropertyInfo ConfigProp)>();

        public object GetDynamicParameters()
        {
            var workspaceVal = Workspace ?? this.GetUnboundValue<string>(nameof(Workspace), 0) ?? string.Empty;

            if (workspaceVal.Equals(InitializeWorkspaceCommand.WORKSPACE_CONSOLE, StringComparison.OrdinalIgnoreCase))
            {
                workspaceVal = InitializeWorkspaceCommand.WORKSPACE_CONSOLE;
            }
            else
            {
                workspaceVal = InitializeWorkspaceCommand.WORKSPACE_DEVELOPMENT;
            }


            foreach (var configInfo in Config.ConfigPropInfo)
            {
                var paramAttr = new ParameterAttribute
                {
                    HelpMessage = configInfo.Description
                };

                if (workspaceVal == InitializeWorkspaceCommand.WORKSPACE_CONSOLE)
                {
                    if (configInfo.Property.Name == nameof(Config.NetworkHost))
                    {
                        paramAttr.Mandatory = true;
                    }
                    else if (configInfo.Property.Name == nameof(Config.NetworkPort))
                    {
                        paramAttr.Mandatory = true;
                    }
                }

                var psDefaultValAttr = new PSDefaultValueAttribute
                {
                    Value = configInfo.DefaultValue,
                    Help = configInfo.Description
                };

                var defaultValAttr = new DefaultValueAttribute(configInfo.DefaultValue);

                var runtimeParam = this.AddRuntimeParam(configInfo.PropertyType, configInfo.Name, paramAttr, psDefaultValAttr, defaultValAttr);
                _props.Add((runtimeParam, configInfo));
            }

            return this.GetRuntimeParams();
        }

        protected override void EndProcessing()
        {
            var config = this.ReadConfig();

            foreach (var prop in _props)
            {
                if (prop.Param.IsSet)
                {
                    prop.ConfigProp.Property.SetValue(config, prop.Param.Value);
                }
            }

            config.Save(SessionState.Path.CurrentLocation.Path);

            WriteObject(config);
        }
    }
}
