using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jukebox
{
    internal class RiqEntity2Converter : JsonConverter<RiqEntity2>
    {
        List<string> datamodels;
        List<string> types;
        public RiqEntity2Converter SetDatamodelsArray(List<string> datamodels)
        {
            this.datamodels = datamodels;
            return this;
        }

        public RiqEntity2Converter SetTypesArray(List<string> types)
        {
            this.types = types;
            return this;
        }

        public override RiqEntity2 ReadJson(JsonReader reader, Type objectType, RiqEntity2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject obj = JObject.Load(reader);

            int typeindex = obj["type"].Value<int>();
            int version = obj["version"].Value<int>();
            int datamodelindex = obj["model"].Value<int>();

            RiqEntity2 entity = new(types[typeindex], datamodels[datamodelindex], version);

            JObject dynamicData = obj["data"].Value<JObject>();

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
            writer.WriteValue(value.SerializedTypeIndex);

            writer.WritePropertyName("version");
            writer.WriteValue(value.Version);

            writer.WritePropertyName("model");
            writer.WriteValue(value.SerializedDatamodelIndex);

            writer.WritePropertyName("data");
            writer.WriteStartObject();
            foreach (RiqHashedKey key in value.Keys)
            {
                writer.WritePropertyName(key.StringValue);
                serializer.Serialize(writer, value.DynamicData[key.Hash], value.DynamicData[key.Hash].GetType());
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
    }
}