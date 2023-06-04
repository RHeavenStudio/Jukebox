using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Jukebox
{
    [Serializable]
    public struct RiqEntity
    {
        public string type;
        public int version;
        public string datamodel;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)] public double beat;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)] public float length;
        [NonSerialized] public int uid;
        public Dictionary<string, dynamic> dynamicData;

        public RiqEntity(string type = "", int version = 0, string datamodel = "", double beat = 0, float length = 0, Dictionary<string, dynamic> dynamicData = null)
        {
            this.type = type;
            this.version = version;
            this.datamodel = datamodel;
            this.beat = beat;
            this.length = length;
            this.dynamicData = dynamicData ?? new();
            uid = RiqBeatmap.UidProvider;
        }

        public RiqEntity DeepCopy()
        {
            RiqEntity copy = new RiqEntity();
            copy.type = type;
            copy.version = version;
            copy.beat = beat;
            copy.length = length;
            copy.datamodel = datamodel;
            copy.dynamicData = new Dictionary<string, dynamic>(dynamicData);
            
            uid = RiqBeatmap.UidProvider;
            return copy;
        }

        public dynamic this[string propertyName]
        {
            get
            {
                switch (propertyName)
                {
                    case "beat":
                        return beat;
                    case "length":
                        return length;
                    case "datamodel":
                        return datamodel;
                    default:
                        if (dynamicData.ContainsKey(propertyName))
                            return dynamicData[propertyName];
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
                        if (dynamicData.ContainsKey(propertyName))
                            dynamicData[propertyName] = value;
                        else
                            throw new Exception($"This entity does not have a property named {propertyName}! Attempted to insert value of type {value.GetType()}");
                        break;
                }
            }
        }

        public void CreateProperty(string name, dynamic defaultValue)
        {
            if (dynamicData == null)
                dynamicData = new();
            
            if (!dynamicData.ContainsKey(name))
                dynamicData.Add(name, defaultValue);
        }
    }
}
