namespace Jukebox
{
    public class RiqHashedKey
    {
        public string StringValue { get; private set; }
        public int Hash { get; private set; }

        RiqHashedKey(string key)
        {
            StringValue = key;
            Hash = key.GetHashCode(System.StringComparison.OrdinalIgnoreCase);
        }

        public static RiqHashedKey CreateFrom(string key)
        {
            return new RiqHashedKey(key);
        }
    }
}