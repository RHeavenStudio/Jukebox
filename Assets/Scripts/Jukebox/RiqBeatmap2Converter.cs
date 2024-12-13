using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jukebox
{
    internal class RiqBeatmap2Converter : JsonConverter<RiqBeatmap2>
    {
        public override RiqBeatmap2 ReadJson(JsonReader reader, Type objectType, RiqBeatmap2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject obj = JObject.Load(reader);

            int version = obj["version"].Value<int>();
            double offset = obj["offset"].Value<double>();

            RiqBeatmap2 beatmap = new(version);
            beatmap.WithOffset(offset);

            List<string> datamodels = new();
            JObject datamodelsObj = obj["models"].Value<JObject>();
            foreach (KeyValuePair<string, JToken> kvp in datamodelsObj)
            {
                datamodels.Insert(int.Parse(kvp.Key), kvp.Value.Value<string>());
            }

            List<string> types = new();
            JObject typesObj = obj["types"].Value<JObject>();
            foreach (KeyValuePair<string, JToken> kvp in typesObj)
            {
                types.Insert(int.Parse(kvp.Key), kvp.Value.Value<string>());
            }

            JArray entities = obj["entities"].Value<JArray>();
            RiqEntity2Converter entityConverter = new();
            entityConverter
                .SetDatamodelsArray(datamodels)
                .SetTypesArray(types);

            foreach (JToken token in entities)
            {
                RiqEntity2 entity = entityConverter.ReadJson(token.CreateReader(), typeof(RiqEntity2), null, serializer) as RiqEntity2;
                beatmap.AddEntity(entity);
            }

            return beatmap;
        }

        public override void WriteJson(JsonWriter writer, RiqBeatmap2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("version");
            writer.WriteValue(value.Version);

            writer.WritePropertyName("offset");
            writer.WriteValue(value.Offset);

            List<string> datamodels = new();
            List<string> types = new();

            writer.WritePropertyName("entities");
            writer.WriteStartArray();
            foreach (RiqEntity2 entity in value.Entities)
            {
                string datamodel = entity.DatamodelHash.StringValue;
                if (!datamodels.Contains(datamodel))
                {
                    datamodels.Add(datamodel);
                }
                if (!types.Contains(entity.Type))
                {
                    types.Add(entity.Type);
                }
                entity.SerializedDatamodelIndex = datamodels.IndexOf(datamodel);
                entity.SerializedTypeIndex = types.IndexOf(entity.Type);
                serializer.Serialize(writer, entity);
            }
            writer.WriteEndArray();

            writer.WritePropertyName("models");
            writer.WriteStartObject();
            for (int i = 0; i < datamodels.Count; i++)
            {
                writer.WritePropertyName(i.ToString());
                writer.WriteValue(datamodels[i]);
            }
            writer.WriteEndObject();

            writer.WritePropertyName("types");
            writer.WriteStartObject();
            for (int i = 0; i < types.Count; i++)
            {
                writer.WritePropertyName(i.ToString());
                writer.WriteValue(types[i]);
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
    }
}