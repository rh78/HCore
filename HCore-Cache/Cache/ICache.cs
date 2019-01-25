using System;
using System.Threading.Tasks;

namespace HCore.Cache
{
    public interface ICache
    {
        Task StoreAsync(string key, object value, TimeSpan expiresIn);
        Task<T> GetAsync<T>(string key) where T : class;
        Task InvalidateAsync(string key);
    }
}
