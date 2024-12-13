using System;
using System.Collections.Generic;
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
            
            RiqMetadata metadata = new(version, origin);

            if (!obj.ContainsKey("data"))
                return metadata;

            JObject metadataObj = obj["data"].Value<JObject>();
            foreach (KeyValuePair<string, JToken> kvp in metadataObj)
            {
                RiqHashedKey key = RiqHashedKey.CreateFrom(kvp.Key);
                metadata.CreateEntry(key.StringValue, kvp.Value.Value<object>());
            }

            return metadata;
        }

        public override void WriteJson(JsonWriter writer, RiqMetadata value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("version");
            writer.WriteValue(value.Version);

            writer.WritePropertyName("origin");
            writer.WriteValue(value.Origin);

            writer.WritePropertyName("data");
            writer.WriteStartObject();
            foreach (RiqHashedKey key in value.Keys)
            {
                writer.WritePropertyName(key.StringValue);
                serializer.Serialize(writer, value.GetEntry(key.Hash));
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
    }
}