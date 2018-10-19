using Meadow.Contract;
using Meadow.JsonRpc.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Meadow.SolCodeGen.CodeGenerators
{
    class EventHelperGenerator : GeneratorBase
    {
        List<GeneratedEventMetadata> _eventMetadata;

        public EventHelperGenerator(List<GeneratedEventMetadata> eventMetadata, string @namespace) : base(@namespace)
        {
            _eventMetadata = eventMetadata;
        }

        protected override string GenerateUsingDeclarations()
        {
            var sourceAttrTypeName = typeof(GeneratedSolcDataAttribute).FullName;
            var assemblyAttr = $"[assembly: {sourceAttrTypeName}]";

            var usings = $@"
                using System;
                using System.Collections.Generic;
                using System.Globalization;
                using System.Threading.Tasks;
                using Meadow.JsonRpc.Types;

                {assemblyAttr}
            ";
            return usings;
        }

        protected override string GenerateClassDef()
        {
            Dictionary<string, string> eventParsers = new Dictionary<string, string>();
            for (var i = 0; i < _eventMetadata.Count; i++)
            {
                var eventLookupKey = $"{_eventMetadata[i].EventSignatureHash}_{_eventMetadata[i].IndexedArgsCounts.ToString("00", CultureInfo.InvariantCulture)}";
                string caseStmt = $"case \"{eventLookupKey}\": return new {_eventMetadata[i].ClrTypeFullName}(log);";
                if (!eventParsers.ContainsKey(eventLookupKey))
                {
                    eventParsers.Add(eventLookupKey, caseStmt);
                }
                else
                {
                    string commentLine = $"{Environment.NewLine}// Event with duplicate signature: {_eventMetadata[i].ClrTypeFullName}";
                    eventParsers[eventLookupKey] += commentLine;
                }
            }

            string eventParsersString = string.Join(Environment.NewLine, eventParsers.Values);

            return $@"
                public static class ContractEventLogHelper
                {{

                    public static {typeof(EventLog).FullName} Parse(string eventSignatureHash, {typeof(FilterLogObject).FullName} log)
                    {{

                        var eventLog = {typeof(EventLogUtil).FullName}.Parse(eventSignatureHash, log);
                        if (eventLog != null)
                        {{
                            return eventLog;
                        }}

                        // Switch on the event signature hash and the number of indexed event arguments.
                        switch (eventSignatureHash + ""_"" + (log.Topics.Length - 1).ToString(""00"", CultureInfo.InvariantCulture))
                        {{
                            {eventParsersString}
                            default: return null;
                        }}

                    }}

                }}
            ";
        }


    }
}
