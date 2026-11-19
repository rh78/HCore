using StackExchange.Redis;

namespace HCore.Cache
{
    public interface IRedisConnectionPool
    {
        IConnectionMultiplexer GetConnectionMultiplexer();
    }
}
