using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jukebox
{
    internal class RiqEntityConverter : JsonConverter<RiqEntity>
    {
        List<string> datamodels;
        List<string> types;
        public RiqEntityConverter SetDatamodelsArray(List<string> datamodels)
        {
            this.datamodels = datamodels;
            return this;
        }

        public RiqEntityConverter SetTypesArray(List<string> types)
        {
            this.types = types;
            return this;
        }

        public override RiqEntity ReadJson(JsonReader reader, Type objectType, RiqEntity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject obj = JObject.Load(reader);

            int typeindex = obj["type"].Value<int>();
            int version = obj["version"].Value<int>();
            int datamodelindex = obj["model"].Value<int>();

            RiqEntity entity = new(types[typeindex], datamodels[datamodelindex], version);

            JObject dynamicData = obj["data"].Value<JObject>();

            foreach (KeyValuePair<string, JToken> kvp in dynamicData)
            {
                RiqHashedKey key = RiqHashedKey.CreateFrom(kvp.Key);
                entity.Keys.Add(key);
                entity.DynamicData.Add(key.Hash, kvp.Value.ToObject<object>());
            }

            return entity;
        }

        public override void WriteJson(JsonWriter writer, RiqEntity value, JsonSerializer serializer)
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