using System.Reflection.Metadata;

namespace HCore.Cache.Configuration
{
    public class RedisConfiguration
    {
        public string Host { get; set; }

        public string Port { get; set; }

        public int ConnectRetry { get; set; }

        public int ConnectTimeout { get; set; }
    }
}
