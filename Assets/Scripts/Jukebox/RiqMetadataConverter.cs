using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jukebox
{
    internal class RiqMetadataConverter : JsonConverter<RiqMetadata>
    {
        public override RiqMetadata ReadJson(JsonReader reader, Type objectType, RiqMetadata existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            int version;
            string origin = "UNKNOWN";

            JObject obj = JObject.Load(reader);

            version = obj["version"].Value<int>();

            if (obj.ContainsKey("origin"))
                origin = obj["origin"].Value<string>();

            return new RiqMetadata(version, origin);
        }

        public override void WriteJson(JsonWriter writer, RiqMetadata value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("version");
            writer.WriteValue(value.Version);

            writer.WritePropertyName("origin");
            writer.WriteValue(value.Origin);

            writer.WriteEndObject();
        }
    }
}