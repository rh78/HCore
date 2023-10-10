using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Cache
{
    public interface ICache
    {
        Task StoreAsync<T>(string key, T value, TimeSpan expiresIn);

        Task<string> GetStringAsync(string key);
        Task<T> GetObjectAsync<T>(string key) where T : class;

        Task<IDictionary<string, string>> GetStringsAsync(IEnumerable<string> keys);

        Task<IDictionary<string, T>> GetObjectsAsync<T>(IEnumerable<string> keys) where T : class;

        Task InvalidateAsync(string key);
        
        Task<bool?> IsAvailableAsync(CancellationToken cancellationToken = default);
    }
}
