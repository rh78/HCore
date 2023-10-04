using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Cache
{
    public interface ICache
    {
        Task StoreAsync(string key, object value, TimeSpan expiresIn);

        Task<string> GetStringAsync(string key);
        Task<T> GetObjectAsync<T>(string key) where T : class;

        Task<IDictionary<string, string>> GetStringsAsync(IEnumerable<string> keys);

        Task<IDictionary<string, T>> GetObjectsAsync<T>(IEnumerable<string> keys) where T : class;

        Task InvalidateAsync(string key);
        
        void Store(string key, object value, TimeSpan expiresIn);

        string GetString(string key);

        T GetObject<T>(string key) where T : class;

        Task<bool?> IsAvailableAsync(CancellationToken cancellationToken = default);
    }
}
