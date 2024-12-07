using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

using UnityEngine;

namespace Jukebox
{
    [JsonConverter(typeof(RiqEntity2Converter))]
    public class RiqEntity2
    {
        public RiqHashedKey DatamodelHash { get; private set; }
        public string Type { get; private set; }
        public int Version { get; private set; }

        public Guid Guid { get; private set; }

        readonly List<RiqHashedKey> keys = new();
        readonly Dictionary<int, object> dynamicData = new();

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

        public RiqEntity2(string type, string datamodel, int version)
        {
            this.Type = type;
            this.DatamodelHash = RiqHashedKey.CreateFrom(datamodel);
            this.Version = version;
            this.Guid = Guid.NewGuid();
        }

        public RiqEntity2(string type, RiqHashedKey datamodel, int version)
        {
            this.Type = type;
            this.DatamodelHash = datamodel;
            this.Version = version;
            
            this.Guid = Guid.NewGuid();
        }

        public RiqEntity2 DeepCopy()
        {
            RiqEntity2 copy = new RiqEntity2(Type, DatamodelHash, Version);
            foreach (KeyValuePair<int, object> kvp in dynamicData)
            {
                copy.dynamicData.Add(kvp.Key, kvp.Value);
            }
            return copy;
        }

        public RiqHashedKey CreateProperty(string name, object defaultValue)
        {
            RiqHashedKey key = RiqHashedKey.CreateFrom(name);
            if (!keys.Contains(key))
            {
                keys.Add(key);
                dynamicData.Add(key.Hash, defaultValue);
            }
            else if (dynamicData[key.Hash] == null)
            {
                dynamicData[key.Hash] = defaultValue;
            }
            UnityEngine.Debug.Log($"Created property {name} (hash: {key.Hash})");
            return key;
        }
    }
}