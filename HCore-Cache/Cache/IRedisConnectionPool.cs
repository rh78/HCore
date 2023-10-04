using StackExchange.Redis;

namespace HCore.Cache
{
    internal interface IRedisConnectionPool
    {
        IConnectionMultiplexer GetConnectionMultiplexer();
    }
}
