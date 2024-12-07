using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jukebox
{
    public class RiqEntity2Converter : JsonConverter<RiqEntity2>
    {
        public override RiqEntity2 ReadJson(JsonReader reader, Type objectType, RiqEntity2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            
            JObject obj = JObject.Load(reader);

            string type = obj["type"].Value<string>();
            string datamodel = obj["datamodel"].Value<string>();
            int version = obj["version"].Value<int>();

            RiqEntity2 entity = new(type, datamodel, version);

            JObject dynamicData = obj["dynamicData"].Value<JObject>();

            foreach (KeyValuePair<string, JToken> kvp in dynamicData)
            {
                RiqHashedKey key = RiqHashedKey.CreateFrom(kvp.Key);
                entity.Keys.Add(key);
                entity.DynamicData.Add(key.Hash, kvp.Value.ToObject<object>());
            }

            return entity;
        }

        public override void WriteJson(JsonWriter writer, RiqEntity2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("type");
            writer.WriteValue(value.Type);

            writer.WritePropertyName("datamodel");
            writer.WriteValue(value.DatamodelHash.StringValue);

            writer.WritePropertyName("version");
            writer.WriteValue(value.Version);

            writer.WritePropertyName("dynamicData");
            writer.WriteStartObject();
            foreach (KeyValuePair<int, object> kvp in value.DynamicData)
            {
                RiqHashedKey k = value.Keys.Find(k => k.Hash == kvp.Key);

                if (k == null)
                {
                    continue;
                }

                writer.WritePropertyName(k.StringValue);
                serializer.Serialize(writer, kvp.Value);
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
    }
}