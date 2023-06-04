using System;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Jukebox
{
    public class RiqEntityConverter : JsonConverter<RiqEntity>
    {
        public override void WriteJson(JsonWriter writer, RiqEntity value, JsonSerializer serializer)
        {
            RiqEntityData dat = value.data;

            writer.WriteRawValue(dat.Serialize());
        }

        public override RiqEntity ReadJson(JsonReader reader, Type objectType, RiqEntity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            JObject obj = JObject.Load(reader);
            RiqEntityData dat = obj.ToObject<RiqEntityData>();
            RiqEntity e = new RiqEntity(dat);

            return e;
        }
    }

    [JsonConverter(typeof(RiqEntityConverter))]
    public class RiqEntity
    {
        public RiqEntityData data;
        public int uid;

        public string datamodel { get => data.datamodel; set => data.datamodel = value; }
        public double beat { get => data.beat; set => data.beat = value; }
        public float length { get => data.length; set => data.length = value; }
        public int version { get => data.version; set => data.version = value; }
        public Dictionary<string, dynamic> dynamicData { get => data.dynamicData; set => data.dynamicData = value; }

        public RiqEntity(string type = "", int version = 0, string datamodel = "", double beat = 0, float length = 0, Dictionary<string, dynamic> dynamicData = null)
        {
            this.data = new RiqEntityData(type, version, datamodel, beat, length, dynamicData);
        }

        public RiqEntity(RiqEntityData data)
        {
            this.data = data;
            uid = RiqBeatmap.UidProvider;
        }

        public RiqEntity DeepCopy()
        {
            RiqEntity copy = new RiqEntity(data.DeepCopy());
            return copy;
        }

        public dynamic this[string propertyName]
        {
            get
            {
                switch (propertyName)
                {
                    case "beat":
                        return data.beat;
                    case "length":
                        return data.length;
                    case "datamodel":
                        return data.datamodel;
                    default:
                        if (data.dynamicData == null)
                            return null;
                        if (data.dynamicData.ContainsKey(propertyName))
                            return data.dynamicData[propertyName];
                        else
                        {
                            return null;
                        }
                }
            }
            set
            {
                switch (propertyName)
                {
                    case "beat":
                    case "length":
                    case "datamodel":
                        throw new Exception($"Property name {propertyName} is reserved and cannot be set.");
                    default:
                        if (data.dynamicData == null)
                            data.dynamicData = new();
                        if (data.dynamicData.ContainsKey(propertyName))
                            data.dynamicData[propertyName] = value;
                        else
                            throw new Exception($"This entity does not have a property named {propertyName}! Attempted to insert value of type {value.GetType()}");
                        break;
                }
            }
        }

        public void CreateProperty(string name, dynamic defaultValue)
        {
            if (data.dynamicData == null)
                data.dynamicData = new();
            
            if (!data.dynamicData.ContainsKey(name))
                data.dynamicData.Add(name, defaultValue);
        }
    }

    [Serializable]
    public struct RiqEntityData
    {
        public string type;
        public int version;
        public string datamodel;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)] public double beat;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)] public float length;
        public Dictionary<string, dynamic> dynamicData;

        public RiqEntityData(string type = "", int version = 0, string datamodel = "", double beat = 0, float length = 0, Dictionary<string, dynamic> dynamicData = null)
        {
            this.type = type;
            this.version = version;
            this.datamodel = datamodel;
            this.beat = beat;
            this.length = length;
            this.dynamicData = dynamicData ?? new();
        }

        public RiqEntityData DeepCopy()
        {
            RiqEntityData copy = new RiqEntityData();
            copy.type = type;
            copy.version = version;
            copy.beat = beat;
            copy.length = length;
            copy.datamodel = datamodel;
            copy.dynamicData = new Dictionary<string, dynamic>(dynamicData);
            
            return copy;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Include,
            });
        }
    }
}
