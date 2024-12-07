using System;
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

            JArray entities = obj["entities"].Value<JArray>();

            foreach (JToken token in entities)
            {
                RiqEntity2 entity = serializer.Deserialize<RiqEntity2>(token.CreateReader());
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

            writer.WritePropertyName("entities");
            writer.WriteStartArray();
            foreach (RiqEntity2 entity in value.Entities)
            {
                serializer.Serialize(writer, entity);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}