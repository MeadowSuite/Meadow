using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Runtime.CompilerServices;

namespace Meadow.Cli
{
    static class IDynamicParametersExtentions
    {
        static readonly ConditionalWeakTable<IDynamicParameters, RuntimeDefinedParameterDictionary> _memMap = new ConditionalWeakTable<IDynamicParameters, RuntimeDefinedParameterDictionary>();

        public static RuntimeDefinedParameterDictionary GetRuntimeParams(this IDynamicParameters cmdlet)
        {
            if (!_memMap.TryGetValue(cmdlet, out var dict))
            {
                dict = new RuntimeDefinedParameterDictionary();
                _memMap.Add(cmdlet, dict);
            }

            return dict;
        }

        public static RuntimeDefinedParameter AddRuntimeParamWithValidateSet<TValue>(this IDynamicParameters cmdlet, string paramName, string[] validSets, ParameterAttribute paramAttribute = null, Attribute[] extraAddtributes = null)
        {
            var attrs = new List<Attribute>() { new ValidateSetAttribute(validSets) };
            if (extraAddtributes != null)
            {
                attrs.AddRange(extraAddtributes);
            }

            return AddRuntimeParam<TValue>(cmdlet, paramName, paramAttribute, attrs.ToArray());
        }

        public static RuntimeDefinedParameter AddRuntimeParam<TValue>(this IDynamicParameters cmdlet, string paramName, params Attribute[] extraAddtributes)
        {
            return AddRuntimeParam<TValue>(cmdlet, paramName, paramAttribute: null, extraAddtributes);
        }

        public static RuntimeDefinedParameter AddRuntimeParam<TValue>(this IDynamicParameters cmdlet, string paramName, ParameterAttribute paramAttribute, params Attribute[] extraAddtributes)
        {
            return AddRuntimeParam(cmdlet, typeof(TValue), paramName, paramAttribute, extraAddtributes);
        }


        public static RuntimeDefinedParameter AddRuntimeParam(this IDynamicParameters cmdlet, Type valueType, string paramName, ParameterAttribute paramAttribute, params Attribute[] extraAddtributes)
        {
            var attrCollection = new Collection<Attribute>() { paramAttribute ?? new ParameterAttribute() };
            if (extraAddtributes != null)
            {
                foreach (var extraParam in extraAddtributes)
                {
                    attrCollection.Add(extraParam);
                }
            }

            var definedParam = new RuntimeDefinedParameter(paramName, valueType, attrCollection);
            var runtimeParams = GetRuntimeParams(cmdlet);
            runtimeParams.Add(paramName, definedParam);
            return definedParam;
        }
    }
}
