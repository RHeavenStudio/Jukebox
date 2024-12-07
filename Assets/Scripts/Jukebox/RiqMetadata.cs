using Newtonsoft.Json;

namespace Jukebox
{
    [JsonConverter(typeof(RiqMetadataConverter))]
    public class RiqMetadata
    {
        public int Version { get; private set; }
        public string Origin { get; private set; }

        public RiqMetadata(int version = 2, string origin = "Jukebox")
        {
            Version = version;
            Origin = origin;
        }
    }
}