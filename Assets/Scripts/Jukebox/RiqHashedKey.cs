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

        /// <summary>
        /// Create a new <see cref="RiqHashedKey"/> object from a key string.
        /// </summary>
        /// <param name="key">The key string</param>
        /// <returns>A new <see cref="RiqHashedKey"/> object representing the key</returns>
        public static RiqHashedKey CreateFrom(string key)
        {
            return new RiqHashedKey(key);
        }
    }
}