using Newtonsoft.Json;
using System;

namespace SolcNet.DataDescription.Parsing
{
    public class NamedStringToken : IEquatable<NamedStringToken>
    {
        public virtual string Value { get; set; }

        public bool Equals(NamedStringToken other) => other?.Value == Value;
        public override bool Equals(object obj) => obj is NamedStringToken other ? other?.Value == Value : false;
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(NamedStringToken a, NamedStringToken b) => a?.Value == b?.Value;
        public static bool operator !=(NamedStringToken a, NamedStringToken b) => !(a == b);

        public override string ToString() => Value;
    }

    class NamedStringTokenConverter<TToken> : JsonConverter<TToken> where TToken : NamedStringToken, new()
    {
        public override TToken ReadJson(JsonReader reader, Type objectType, TToken existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new TToken { Value = (string)reader.Value };
        }

        public override void WriteJson(JsonWriter writer, TToken value, JsonSerializer serializer)
        {
            writer.WriteToken(JsonToken.String, value.Value);
        }
    }
}
