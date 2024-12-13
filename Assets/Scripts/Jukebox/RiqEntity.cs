using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Jukebox
{
    [JsonConverter(typeof(RiqEntityConverter))]
    public class RiqEntity
    {
        public RiqHashedKey DatamodelHash { get; private set; }
        public string Type { get; private set; }
        public int Version { get; private set; }

        public Guid Guid { get; private set; }

        readonly List<RiqHashedKey> keys = new();
        readonly Dictionary<int, object> dynamicData = new();

        public int SerializedDatamodelIndex;
        public int SerializedTypeIndex;

        public List<RiqHashedKey> Keys { get => keys; }
        public Dictionary<int, object> DynamicData { get => dynamicData; }

        [Obsolete("Use this.DatamodelHash.StringValue instead, or use the search operations in <see cref=\"RiqBeatmap2\"/>.")]
        public string datamodel { get => DatamodelHash.StringValue; set => DatamodelHash = RiqHashedKey.CreateFrom(value); }
        [Obsolete("Use this[\"beat\"] instead.")]
        public double beat { get => this["beat"] is null ? 0.0d : (double)this["beat"] ; set => this["beat"] = value; }
        [Obsolete("Use this[\"length\"] instead.")]
        public float length { get => this["length"] is null ? 0.0f : (float)this["length"]; set => this["length"] = value; }

        public object this[string propertyName]
        {
            get
            {
                RiqHashedKey key = RiqHashedKey.CreateFrom(propertyName);
                if (keys.Contains(key))
                {
                    return dynamicData[key.Hash];
                }
                return null;
            }
            set
            {
                RiqHashedKey key = RiqHashedKey.CreateFrom(propertyName);
                if (keys.Contains(key))
                {
                    dynamicData[key.Hash] = value;
                }
                else
                {
                    keys.Add(key);
                    dynamicData.Add(key.Hash, value);
                }
            }
        }

        public object this[int hash]
        {
            get
            {
                if (dynamicData.ContainsKey(hash))
                {
                    return dynamicData[hash];
                }
                return null;
            }
            set
            {
                dynamicData[hash] = value;
            }
        }

        public RiqEntity(string type, string datamodel, int version)
        {
            this.Type = type;
            this.DatamodelHash = RiqHashedKey.CreateFrom(datamodel);
            this.Version = version;

            this.Guid = Guid.NewGuid();
        }

        public RiqEntity(string type, RiqHashedKey datamodel, int version)
        {
            this.Type = type;
            this.DatamodelHash = datamodel;
            this.Version = version;
            
            this.Guid = Guid.NewGuid();
        }

        public RiqEntity DeepCopy()
        {
            RiqEntity copy = new RiqEntity(Type, DatamodelHash, Version);
            copy.dynamicData.Clear();
            foreach (RiqHashedKey key in keys)
            {
                copy.CreateProperty(key, dynamicData[key.Hash]);
            }
            return copy;
        }

        public RiqHashedKey CreateProperty(string name, object defaultValue)
        {
            RiqHashedKey key = RiqHashedKey.CreateFrom(name);
            return CreateProperty(key, defaultValue);
        }

        public RiqHashedKey CreateProperty(RiqHashedKey key, object defaultValue)
        {
            if (!keys.Contains(key))
            {
                keys.Add(key);
                dynamicData.Add(key.Hash, defaultValue);
            }
            else if (dynamicData[key.Hash] == null)
            {
                dynamicData[key.Hash] = defaultValue;
            }
            UnityEngine.Debug.Log($"Created property {key.StringValue} (hash: {key.Hash})");
            return key;
        }

        public RiqEntity AddProperty(string name, object defaultValue, out RiqHashedKey key)
        {
            key = CreateProperty(name, defaultValue);
            return this;
        }

        public RiqEntity AddProperty(RiqHashedKey key, object defaultValue, out RiqHashedKey keyOut)
        {
            keyOut = CreateProperty(key, defaultValue);
            return this;
        }
    }
}