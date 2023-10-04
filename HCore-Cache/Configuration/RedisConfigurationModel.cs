using System.Collections.Generic;
using System.Security.Permissions;
using StackExchange.Redis;

namespace HCore.Cache.Configuration
{
    public class RedisConfigurationModel
    {
        private const string _defaultHost = "127.0.0.1";
        private const int _defaultPort = 6379;

        public ICollection<RedisHostConfigurationModel> Hosts { get; set; }

        public string Password { get; set; }

        public int ConnectRetry { get; set; }

        public int ConnectTimeout { get; set; }

        public int PoolSize { get; set; } = 1;

        public ConfigurationOptions GetConfigurationOptions()
        {
            Hosts ??= new List<RedisHostConfigurationModel>();

            if (Hosts.Count == 0)
            {
                Hosts.Add(new RedisHostConfigurationModel
                {
                    Host = _defaultHost,
                    Port = _defaultPort
                });
            }

            var configurationOptions = new ConfigurationOptions
            {
                Password = Password,
                ConnectRetry = ConnectRetry,
                ConnectTimeout = ConnectTimeout
            };

            foreach (var host in Hosts)
            {
                configurationOptions.EndPoints.Add(host.Host, host.Port);
            }

            return configurationOptions;
        }
    }
}
