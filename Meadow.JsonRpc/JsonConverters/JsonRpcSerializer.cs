using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;

namespace Meadow.JsonRpc.JsonConverters
{
    public static class JsonRpcSerializer
    {
        public static readonly JsonSerializerSettings Settings;
        public static readonly JsonSerializer Serializer;
        public static readonly JsonConverter[] Converters = new JsonConverter[]
        {
            new AddressHexJsonConverter(),
            new DataHexJsonConverter(),
            new HashHexJsonConverter(),
            new UInt256HexJsonConverter(),
            new JsonRpcHexConverter()
        };

        static JsonRpcSerializer()
        {
            Settings = new JsonSerializerSettings();
            Settings.DefaultValueHandling = DefaultValueHandling.Ignore;

            foreach (var converter in Converters)
            {
                Settings.Converters.Add(converter);
            }

            Serializer = JsonSerializer.CreateDefault(Settings);
        }
    }
}
