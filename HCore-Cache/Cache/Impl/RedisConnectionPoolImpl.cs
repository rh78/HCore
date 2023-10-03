using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HCore.Cache.Configuration;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace HCore.Cache.Impl
{
    internal class RedisConnectionPoolImpl : IRedisConnectionPool, IDisposable
    {
        private readonly RedisConfigurationModel _redisConfigurationModel;

        private readonly ICollection<ConnectionMultiplexer> _connectionMultiplexers = new List<ConnectionMultiplexer>();

        private long _currentIndex = -1;

        private bool _disposed;

        public RedisConnectionPoolImpl(IConfiguration configuration)
        {
            _redisConfigurationModel = configuration.GetSection("Cache:Redis").Get<RedisConfigurationModel>();

            if (_redisConfigurationModel == null)
            {
                throw new InvalidOperationException("Missing 'Cache:Redis' configuration");
            }

            InitConnectionMultiplexers();
        }

        private void InitConnectionMultiplexers()
        {
            var configurationOptions = _redisConfigurationModel.GetConfigurationOptions();

            for (var i = 0; i < _redisConfigurationModel.PoolSize; i++)
            {
                var connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);

                _connectionMultiplexers.Add(connectionMultiplexer);
            }
        }

        public IConnectionMultiplexer GetConnectionMultiplexer()
        {
            var index = Interlocked.Increment(ref _currentIndex);

            index %= _connectionMultiplexers.Count;

            return _connectionMultiplexers.ElementAt(index);
        }

        public void Dispose()
        {
            Dispose(disposing: true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (!disposing)
            {
                return;
            }

            foreach (var connectionMultiplexer in _connectionMultiplexers)
            {
                connectionMultiplexer.Dispose();
            }

            _disposed = true;
        }
    }
}
