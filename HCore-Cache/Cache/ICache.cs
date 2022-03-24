using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCore.Cache
{
    public interface ICache
    {
        Task StoreAsync(string key, object value, TimeSpan expiresIn);

        Task<T> GetAsync<T>(string key) where T : class;

        Task<IDictionary<string, T>> GetAsync<T>(IEnumerable<string> keys) where T : class;

        Task InvalidateAsync(string key);
        
        void Store(string key, object value, TimeSpan expiresIn);

        T Get<T>(string key) where T : class;
    }
}
