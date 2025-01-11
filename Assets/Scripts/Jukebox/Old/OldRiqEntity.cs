#if JUKEBOX_V1
using System;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Jukebox.Legacy
{
    public class OldRiqEntityConverter : JsonConverter<OldRiqEntity>
    {
        public override void WriteJson(JsonWriter writer, OldRiqEntity value, JsonSerializer serializer)
        {
            OldRiqEntityData dat = value.data;

            writer.WriteRawValue(dat.Serialize());
        }

        public override OldRiqEntity ReadJson(JsonReader reader, Type objectType, OldRiqEntity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            JObject obj = JObject.Load(reader);
            OldRiqEntityData dat = obj.ToObject<OldRiqEntityData>();
            OldRiqEntity e = new OldRiqEntity(dat);

            return e;
        }
    }

    [JsonConverter(typeof(OldRiqEntityConverter))]
    public class OldRiqEntity
    {
        public OldRiqEntityData data;
        public int uid;
        public Guid guid;

        public string datamodel { get => data.datamodel; set => data.datamodel = value; }
        public double beat { get => data.beat; set => data.beat = value; }
        public float length { get => data.length; set => data.length = value; }
        public int version { get => data.version; set => data.version = value; }

#if ENABLE_IL2CPP
        public Dictionary<string, object> dynamicData { get => data.dynamicData; set => data.dynamicData = value; }

        public OldRiqEntity(string type = "", int version = 0, string datamodel = "", double beat = 0, float length = 0, Dictionary<string, object> dynamicData = null)
        {
#else
        public Dictionary<string, dynamic> dynamicData { get => data.dynamicData; set => data.dynamicData = value; }

        public OldRiqEntity(string type = "", int version = 0, string datamodel = "", double beat = 0, float length = 0, Dictionary<string, dynamic> dynamicData = null)
        {
#endif
            this.guid = Guid.NewGuid();
            this.data = new OldRiqEntityData(type, version, datamodel, beat, length, dynamicData);
            this.uid = OldRiqBeatmap.UidProvider;
        }

        public OldRiqEntity(OldRiqEntityData data)
        {
            this.guid = Guid.NewGuid();
            this.data = data;
            this.uid = OldRiqBeatmap.UidProvider;
        }

        public OldRiqEntity DeepCopy()
        {
            OldRiqEntity copy = new OldRiqEntity(data.DeepCopy());
            return copy;
        }

#if ENABLE_IL2CPP
        public object this[string propertyName]
#else
        public dynamic this[string propertyName]
#endif
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

#if ENABLE_IL2CPP
        public void CreateProperty(string name, object defaultValue)
#else
        public void CreateProperty(string name, dynamic defaultValue)
#endif
        {
            if (data.dynamicData == null)
                data.dynamicData = new();

            if (!data.dynamicData.ContainsKey(name))
            {
                data.dynamicData.Add(name, defaultValue);
            }
            else if (data.dynamicData[name] == null)
            {
                data.dynamicData[name] = defaultValue;
            }
        }
    }

    [Serializable]
    public struct OldRiqEntityData
    {
        public string type;
        public int version;
        public string datamodel;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)] public double beat;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)] public float length;

#if ENABLE_IL2CPP
        public Dictionary<string, object> dynamicData;
        public OldRiqEntityData(string type = "", int version = 0, string datamodel = "", double beat = 0, float length = 0, Dictionary<string, object> dynamicData = null)
        {
#else
        public Dictionary<string, dynamic> dynamicData;
        public OldRiqEntityData(string type = "", int version = 0, string datamodel = "", double beat = 0, float length = 0, Dictionary<string, dynamic> dynamicData = null)
        {
#endif        
            this.type = type;
            this.version = version;
            this.datamodel = datamodel;
            this.beat = beat;
            this.length = length;
            this.dynamicData = dynamicData ?? new();
        }

        public OldRiqEntityData DeepCopy()
        {
            OldRiqEntityData copy = new OldRiqEntityData();
            copy.type = type;
            copy.version = version;
            copy.beat = beat;
            copy.length = length;
            copy.datamodel = datamodel;
#if ENABLE_IL2CPP
            copy.dynamicData = new Dictionary<string, object>(dynamicData);
#else
            copy.dynamicData = new Dictionary<string, dynamic>(dynamicData);
#endif

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

#endif