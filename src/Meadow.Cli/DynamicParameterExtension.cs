using ExposedObject;
using System;
using System.Globalization;
using System.Management.Automation;
using System.Reflection;

// Originally taken from https://stackoverflow.com/a/35196839/794962

namespace Meadow.Cli
{
    public static class DynamicParameterExtension
    {
        /// <summary>
        /// Gets the private variable of type System.Management.Automation.ExecutionContext
        /// </summary>
        public static object GetExecutionContext(this PSCmdlet cmdlet)
        {
            var context = Exposed.From(cmdlet).Context;
            return context;
        }

        public static T GetUnboundValue<T>(this PSCmdlet cmdlet, string paramName, int unnamedPosition = -1)
        {
            var context = GetExecutionContext(cmdlet);
            var processor = Exposed.From(context).CurrentCommandProcessor;
            var parameterBinder = Exposed.From(processor).CmdletParameterBinderController;
            var args = Exposed.From(parameterBinder).UnboundArguments as System.Collections.IEnumerable;

            if (args != null)
            {
                var isSwitch = typeof(SwitchParameter) == typeof(T);

                var currentParameterName = string.Empty;
                object unnamedValue = null;
                var i = 0;
                foreach (var arg in args)
                {
                    var isParameterName = Exposed.From(arg).ParameterNameSpecified;
                    if (isParameterName != null && true.Equals(isParameterName))
                    {
                        var parameterName = Exposed.From(arg).ParameterName as string;
                        currentParameterName = parameterName;
                        if (isSwitch && string.Equals(currentParameterName, paramName, StringComparison.OrdinalIgnoreCase))
                        {
                            return (T)(object)new SwitchParameter(true);
                        }

                        continue;
                    }

                    var parameterValue = Exposed.From(arg).ArgumentValue;

                    if (!string.IsNullOrEmpty(currentParameterName))
                    {
                        if (string.Equals(currentParameterName, paramName, StringComparison.OrdinalIgnoreCase))
                        {
                            return ConvertParameter<T>(parameterValue);
                        }
                    }
                    else if (i++ == unnamedPosition)
                    {
                        unnamedValue = parameterValue;
                    }

                    currentParameterName = string.Empty;
                }

                if (unnamedValue != null)
                {
                    return ConvertParameter<T>(unnamedValue);
                }
            }

            return default;
        }

        static T ConvertParameter<T>(this object value)
        {
            if (value == null || Equals(value, default(T)))
            {
                return default;
            }

            var psObject = value as PSObject;
            if (psObject != null)
            {
                return psObject.BaseObject.ConvertParameter<T>();
            }

            if (value is T)
            {
                return (T)value;
            }

            var constructorInfo = typeof(T).GetConstructor(new[] { value.GetType() });
            if (constructorInfo != null)
            {
                return (T)constructorInfo.Invoke(new[] { value });
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
