using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace Meadow.Cli
{
    static class CmdLetExtensions
    {
        const string ENV_PREFIX = "env:";

        public static Dictionary<string, string> GetEnvironmentVariables(this PSCmdlet cmdlet)
        {
            var envVars = cmdlet.SessionState.InvokeProvider.ChildItem.Get(ENV_PREFIX + "*", false);

            var dict = new Dictionary<string, string>(envVars.Count);

            var dictEntries = envVars
                .Where(val => val.BaseObject is DictionaryEntry)
                .Select(val => (DictionaryEntry)val.BaseObject);

            foreach (var item in dictEntries)
            {
                dict[item.Key.ToString()] = item.Value.ToString();
            }

            return dict;
        }

        /// <param name="key">Case-insensentive variable name without the "env:" prefix</param>
        public static string GetEnvironmentVariable(this PSCmdlet cmdlet, string key)
        {
            var match = cmdlet.SessionState.InvokeProvider.ChildItem.Get(ENV_PREFIX + key, false).SingleOrDefault();
            return (match?.BaseObject is DictionaryEntry dictEntry) ? dictEntry.Value?.ToString() : null;
        }

        public static PSObject[] Execute<TCmdlet>(this PSCmdlet cmdlet, params (string Name, object Value)[] cmdParams) where TCmdlet : PSCmdlet
        {
            var cmdletAttr = typeof(TCmdlet).GetCustomAttribute<CmdletAttribute>();
            var cmdName = cmdletAttr.VerbName + "-" + cmdletAttr.NounName;
            var cmdInfo = cmdlet.InvokeCommand.GetCommand(cmdName, CommandTypes.Cmdlet);

            using (var pwsh = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                var result = pwsh
                    .AddCommand(cmdInfo)
                    .AddParameters(cmdParams.ToDictionary(t => t.Name, t => t.Value))
                    .Invoke()
                    .ToArray();

                return result;
            }
        }

        public static PSObject[] Execute2<TCmdlet>(this PSCmdlet cmdlet) where TCmdlet : PSCmdlet
        {
            var cmdletAttr = typeof(TCmdlet).GetCustomAttribute<CmdletAttribute>();
            var cmdName = cmdletAttr.VerbName + "-" + cmdletAttr.NounName;

            using (var pipeline = Runspace.DefaultRunspace.CreateNestedPipeline())
            {
                pipeline.Commands.Add(cmdName);
                var result = pipeline.Invoke().ToArray();
                return result;
            }
        }

        public static PSObject[] Execute<TCmdlet>() where TCmdlet : PSCmdlet
        {
            var cmdName = GetCmdletName<TCmdlet>();
            using (var pwsh = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                var result = pwsh
                    .AddCommand(cmdName)
                    .Invoke()
                    .ToArray();
                return result;
            }
            
        }

        public static string GetCmdletName<TCmdlet>() where TCmdlet : Cmdlet
        {
            var cmdletAttr = typeof(TCmdlet).GetCustomAttribute<CmdletAttribute>();
            var cmdName = cmdletAttr.VerbName + "-" + cmdletAttr.NounName;
            return cmdName;
        }

        public static Config ReadConfig(this PSCmdlet cmdlet)
        {
            return Config.Read(cmdlet.SessionState.Path.CurrentLocation.Path);
        }

        public static void WriteError(this PSCmdlet cmdlet, string message, ErrorCategory errorCategory)
        {
            var exception = new Exception(message);
            var errorRecord = new ErrorRecord(exception, null, errorCategory, null);
            cmdlet.WriteError(errorRecord);
        }

        public static object Prompt(this PSCmdlet cmdlet, Type objectType, string name, string caption = null, string message = null, bool isMandatory = true, PSObject defaultValue = null, Attribute[] attributes = null)
        {
            var fieldDesc = new FieldDescription(name);
            fieldDesc.IsMandatory = isMandatory;
            fieldDesc.SetParameterType(objectType);

            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    fieldDesc.Attributes.Add(attr);
                }
            }

            if (defaultValue != null)
            {
                fieldDesc.DefaultValue = defaultValue;
            }

            var fieldDescs = new Collection<FieldDescription>(new[] { fieldDesc });
            var result = cmdlet.Host.UI.Prompt(caption ?? $"Type: {objectType.Name}", message, fieldDescs);
            var enteredObject = result.First().Value.BaseObject;
            if (objectType != typeof(string) && enteredObject is string enteredStr && string.IsNullOrEmpty(enteredStr))
            {
                Activator.CreateInstance(objectType);
            }

            return enteredObject;
        }

        public static TObject Prompt<TObject>(this PSCmdlet cmdlet, string name, string caption = null, string message = null, bool isMandatory = true, PSObject defaultValue = null, Attribute[] attributes = null)
        {
            return (TObject)Prompt(cmdlet, typeof(TObject), name, caption, message, isMandatory, defaultValue, attributes);
        }

    }
}
