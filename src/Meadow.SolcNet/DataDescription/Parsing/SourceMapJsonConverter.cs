using Newtonsoft.Json;
using SolcNet.DataDescription.Output;
using System;

namespace SolcNet.DataDescription.Parsing
{
    class SourceMapJsonConverter : JsonConverter<SourceMaps>
    {
        public override SourceMaps ReadJson(JsonReader reader, Type objectType, SourceMaps existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var val = reader.Value as string;
            return new SourceMaps { EncodedValue = val };
        }

        public override void WriteJson(JsonWriter writer, SourceMaps value, JsonSerializer serializer)
        {
            writer.WriteToken(JsonToken.String, value.EncodedValue);
        }
    }
}
