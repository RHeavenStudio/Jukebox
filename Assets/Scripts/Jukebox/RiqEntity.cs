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
                    this[key.Hash] = value;
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
                if (dynamicData.ContainsKey(hash))
                {
                    dynamicData[hash] = value;
                }
                else
                {
                    dynamicData.Add(hash, value);
                }
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

        /// <summary>
        /// Creates a deep copy of this <see cref="RiqEntity"/> object.
        /// </summary>
        /// <returns>A copy of the entity</returns>
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

        /// <summary>
        /// Create a new property with a default value.
        /// </summary>
        /// <param name="key">Key of the property</param>
        /// <param name="defaultValue">Default value of the property</param>
        /// <returns>A <see cref="RiqHashedKey"/> object representing the property's key</returns>
        public RiqHashedKey CreateProperty(string key, object defaultValue)
        {
            RiqHashedKey hashkey = RiqHashedKey.CreateFrom(key);
            return CreateProperty(hashkey, defaultValue);
        }

        /// <summary>
        /// Create a new property with a default value.
        /// </summary>
        /// <param name="key">Key of the property</param>
        /// <param name="defaultValue">Default value of the property</param>
        /// <returns>A <see cref="RiqHashedKey"/> object representing the property's key</returns>
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

        /// <summary>
        /// Add a new property to the entity.
        /// </summary>
        /// <param name="key">Key of the property</param>
        /// <param name="defaultValue">Default value of the property</param>
        /// <param name="hashkey">A <see cref="RiqHashedKey"/> object representing the property's key</returns>
        /// <returns>This <see cref="RiqEntity"/> object</returns>
        public RiqEntity AddProperty(string key, object defaultValue, out RiqHashedKey hashkey)
        {
            hashkey = CreateProperty(key, defaultValue);
            return this;
        }

        /// <summary>
        /// Add a new property to the entity.
        /// </summary>
        /// <param name="key">A <see cref="RiqHashedKey"/> object representing the property's key</param>
        /// <param name="defaultValue">Default value of the property</param>
        /// <returns>This <see cref="RiqEntity"/> object</returns>
        public RiqEntity AddProperty(RiqHashedKey key, object defaultValue)
        {
            CreateProperty(key, defaultValue);
            return this;
        }

        /// <summary>
        /// Tries to get a property from the entity as the specified type.
        /// </summary>
        /// <typeparam name="T">Type to get the property as</typeparam>
        /// <param name="key">A <see cref="RiqHashedKey"/> object representing the property's key</param>
        /// <param name="value">Returned value</param>
        /// <returns>True if the property exists and can be returned as the specified type.</returns>
        public bool TryGetProperty<T>(RiqHashedKey key, out T value)
        {
            return TryGetProperty(key.Hash, out value);
        }

        /// <summary>
        /// Tries to get a property from the entity as the specified type.
        /// </summary>
        /// <typeparam name="T">Type to get the property as</typeparam>
        /// <param name="hash">Name of the property to get</param>
        /// <param name="value">Returned value</param>
        /// <returns>True if the property exists and can be returned as the specified type.</returns>
        public bool TryGetProperty<T>(string key, out T value)
        {
            RiqHashedKey hashkey = RiqHashedKey.CreateFrom(key);
            return TryGetProperty(hashkey, out value);
        }

        /// <summary>
        /// Tries to get a property from the entity as the specified type.
        /// </summary>
        /// <typeparam name="T">Type to get the property as</typeparam>
        /// <param name="hash">Hash of the property key</param>
        /// <param name="value">Returned value</param>
        /// <returns>True if the property exists and can be returned as the specified type.</returns>
        public bool TryGetProperty<T>(int hash, out T value)
        {
            if (dynamicData.ContainsKey(hash))
            {
                value = (T)Convert.ChangeType(dynamicData[hash], typeof(T));
                return true;
            }
            value = default;
            return false;
        }
    }
}