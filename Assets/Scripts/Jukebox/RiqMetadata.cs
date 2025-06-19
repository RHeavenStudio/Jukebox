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
                    CreateEntry(key, value);
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

        /// <summary>
        /// Create a new entry in the metadata.
        /// </summary>
        /// <param name="key">Key of the entry</param>
        /// <param name="value">Value of the entry</param>
        /// <returns>A <see cref="RiqHashedKey"/> object representing the key</returns>
        public RiqHashedKey CreateEntry(string key, object value)
        {
            RiqHashedKey hashedKey = RiqHashedKey.CreateFrom(key);
            metadata.Add(hashedKey.Hash, value);
            return hashedKey;
        }

        /// <summary>
        /// Add a new entry to the metadata.
        /// </summary>
        /// <param name="key">Key of the entry</param>
        /// <param name="value">Value of the entry</param>
        /// <param name="hashedKey">A <see cref="RiqHashedKey"/> object representing the key</returns>
        /// <returns>This <see cref="RiqMetadata"/> object</returns>
        public RiqMetadata AddEntry(string key, object value, out RiqHashedKey hashedKey)
        {
            hashedKey = CreateEntry(key, value);
            return this;
        }
    }
}