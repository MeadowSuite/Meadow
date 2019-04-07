using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SolcNet.DataDescription.Output
{
    public class Doc
    {
        [JsonProperty("methods", ItemConverterType = typeof(MethodDocConverter))]
        public Dictionary<string /*function signature*/, MethodDoc> Methods { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("notice")]
        public string Notice { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }

        public static implicit operator Doc(string json) => JsonConvert.DeserializeObject<Doc>(json);
    }

    class MethodDocConverter : JsonConverter<MethodDoc>
    {
        public override MethodDoc ReadJson(JsonReader reader, Type objectType, MethodDoc existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return new MethodDoc { Notice = (string)reader.Value };
            }
            else
            {
                return serializer.Deserialize<MethodDoc>(reader);
            }
        }

        public override void WriteJson(JsonWriter writer, MethodDoc value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

    }

    public class MethodDoc
    {
        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }

        [JsonProperty("notice")]
        public string Notice { get; set; }

        [JsonProperty("params")]
        public Dictionary<string /*param name*/, string /*description*/> Params { get; set; }

        [JsonProperty("return")]
        public string Return { get; set; }
    }

}
