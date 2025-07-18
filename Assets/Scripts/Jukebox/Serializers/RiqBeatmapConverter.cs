using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jukebox
{
    internal class RiqBeatmapConverter : JsonConverter<RiqBeatmap>
    {
        public override RiqBeatmap ReadJson(JsonReader reader, Type objectType, RiqBeatmap existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject obj = JObject.Load(reader);

            int version = obj["version"].Value<int>();
            double offset = obj["offset"].Value<double>();

            string songName;
            if (obj.ContainsKey("songname"))
            {
                songName = obj["songname"].Value<string>();
            }
            else
            {
                songName = "song0";
                if (version == 2) version = 201;
            }

            RiqBeatmap beatmap = new(version);
            beatmap.WithOffset(offset)
                   .WithSongName(songName);

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
            RiqEntityConverter entityConverter = new();
            entityConverter
                .SetDatamodelsArray(datamodels)
                .SetTypesArray(types);

            foreach (JToken token in entities)
            {
                RiqEntity entity = entityConverter.ReadJson(token.CreateReader(), typeof(RiqEntity), null, serializer) as RiqEntity;
                beatmap.CreateEntity(entity);
            }

            return beatmap;
        }

        public override void WriteJson(JsonWriter writer, RiqBeatmap value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("version");
            writer.WriteValue(value.Version);

            writer.WritePropertyName("offset");
            writer.WriteValue(value.Offset);

            writer.WritePropertyName("songname");
            writer.WriteValue(value.SongName);

            List<string> datamodels = new();
            List<string> types = new();

            writer.WritePropertyName("entities");
            writer.WriteStartArray();
            foreach (RiqEntity entity in value.Entities)
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