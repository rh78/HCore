using System;
using System.Threading.Tasks;

namespace HCore.Redis.Cache
{
    public interface IRedisCache
    {
        Task StoreAsync(string key, object value, TimeSpan? expiresIn = null);
        Task<T> GetAsync<T>(string key) where T : class;        
    }
}
