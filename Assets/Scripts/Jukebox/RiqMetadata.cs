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

        public RiqMetadata(int version = 2, string origin = "Jukebox")
        {
            Version = version;
            Origin = origin;
        }

        public RiqHashedKey CreateEntry(string key, object value)
        {
            RiqHashedKey hashedKey = RiqHashedKey.CreateFrom(key);
            return CreateEntry(hashedKey, value);
        }

        public RiqHashedKey CreateEntry(RiqHashedKey key, object value)
        {
            keys.Add(key);
            metadata.Add(key.Hash, value);
            return key;
        }

        public object GetEntry(string key)
        {
            RiqHashedKey hashedKey = RiqHashedKey.CreateFrom(key);
            if (keys.Contains(hashedKey))
            {
                return metadata[hashedKey.Hash];
            }
            return null;
        }

        public object GetEntry(int hash)
        {
            if (metadata.ContainsKey(hash))
            {
                return metadata[hash];
            }
            return null;
        }
    }
}