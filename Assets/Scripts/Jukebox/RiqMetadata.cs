using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Jukebox
{
    [JsonConverter(typeof(RiqMetadataConverter))]
    public class RiqMetadata
    {
        public int Version { get; private set; }
        public string Origin { get; private set; }

        public List<RiqHashedKey> Keys { get => keys; }
        public Dictionary<int, object> Metadata { get => metadata; }

        readonly List<RiqHashedKey> keys = new();
        readonly Dictionary<int, object> metadata = new();

        public object this[string key]
        {
            get
            {
                RiqHashedKey hashedKey = RiqHashedKey.CreateFrom(key);
                return metadata[hashedKey.Hash];
            }
            set
            {
                RiqHashedKey hashedKey = RiqHashedKey.CreateFrom(key);
                if (metadata.ContainsKey(hashedKey.Hash))
                {
                    metadata[hashedKey.Hash] = value;
                }
                else
                {
                    CreateEntry(hashedKey, value);
                }
            }
        }

        public object this[int key]
        {
            get
            {
                return metadata[key];
            }
            set
            {
                if (metadata.ContainsKey(key))
                {
                    metadata[key] = value;
                }
                else
                {
                    metadata.Add(key, value);
                }
            }
        }

        public RiqMetadata(int version = 201, string origin = "Jukebox")
        {
            Version = version;
            Origin = origin;
        }

        private RiqHashedKey CreateEntry(RiqHashedKey key, object value)
        {
            metadata.Add(key.Hash, value);
            keys.Add(key);
            return key;
        }

        /// <summary>
        /// Add a new entry to the metadata.
        /// </summary>
        /// <param name="key">Key of the entry as a string</param>
        /// <param name="value">Value of the entry</param>
        /// <param name="hashedKey">A <see cref="RiqHashedKey"/> representing the key</returns>
        /// <returns>This <see cref="RiqMetadata"/> object</returns>
        public RiqMetadata AddEntry(string key, object value, out RiqHashedKey hashedKey)
        {
            hashedKey = RiqHashedKey.CreateFrom(key);
            CreateEntry(hashedKey, value);
            return this;
        }

        /// <summary>
        /// Add a new entry to the metadata.
        /// </summary>
        /// <param name="key">Key of the entry as a <see cref="RiqHashedKey"/></param>
        /// <param name="value">Value of the entry</param>
        /// <returns>This <see cref="RiqMetadata"/> object</returns>
        public RiqMetadata AddEntry(RiqHashedKey key, object value)
        {
            CreateEntry(key, value);
            return this;
        }

        /// <summary>
        /// Tries to get an entry from the metadata as the specified type.
        /// </summary>
        /// <typeparam name="T">Type to get the property as</typeparam>
        /// <param name="key">A <see cref="RiqHashedKey"/> object representing the property's key</param>
        /// <param name="value">Returned value</param>
        /// <returns>True if the property exists and can be returned as the specified type.</returns>
        public bool TryGetEntry<T>(RiqHashedKey key, out T value)
        {
            return TryGetEntry(key.Hash, out value);
        }

        /// <summary>
        /// Tries to get an entry from the metadata as the specified type.
        /// </summary>
        /// <typeparam name="T">Type to get the property as</typeparam>
        /// <param name="hash">Name of the property to get</param>
        /// <param name="value">Returned value</param>
        /// <returns>True if the property exists and can be returned as the specified type.</returns>
        public bool TryGetEntry<T>(string key, out T value)
        {
            RiqHashedKey hashkey = RiqHashedKey.CreateFrom(key);
            return TryGetEntry(hashkey, out value);
        }

        /// <summary>
        /// Tries to get an entry from the metadata as the specified type.
        /// </summary>
        /// <typeparam name="T">Type to get the property as</typeparam>
        /// <param name="hash">Hash of the property key</param>
        /// <param name="value">Returned value</param>
        /// <returns>True if the property exists and can be returned as the specified type.</returns>
        public bool TryGetEntry<T>(int hash, out T value)
        {
            if (metadata.ContainsKey(hash))
            {
                value = (T)Convert.ChangeType(metadata[hash], typeof(T));
                return true;
            }
            value = default;
            return false;
        }
    }
}