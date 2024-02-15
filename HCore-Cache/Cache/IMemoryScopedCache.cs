using System;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Cache
{
    public interface IMemoryScopedCache
    {
        /// <summary>
        /// Caches data in server memory - as static
        /// </summary>
        /// <typeparam name="TData">The type of the data</typeparam>
        /// <param name="semaphoreSlim"><see cref="SemaphoreSlim"/> instance</param>
        /// <param name="developerUuid">The developer identifier</param>
        /// <param name="tenantUuid">The tenant identifier</param>
        /// <param name="isDraft">Whether is draft</param>
        /// <param name="key">The key to cache by</param>
        /// <param name="expirationInSeconds">In memory cache expiration in seconds</param>
        /// <param name="setFunc">Func to initialize the data if not found</param>
        /// <param name="version">The version of the record</param>
        /// <returns></returns>
        Task<TData> GetOrSetAsync<TData>(SemaphoreSlim semaphoreSlim, long developerUuid, long tenantUuid, bool isDraft, string key, double expirationInSeconds, Func<Task<TData>> setFunc, int? version = null) where TData : class;

        /// <summary>
        /// Caches data in server memory - as static or per request/scope
        /// </summary>
        /// <typeparam name="TData">The type of the data</typeparam>
        /// <param name="semaphoreSlim"><see cref="SemaphoreSlim"/> instance</param>
        /// <param name="developerUuid">The developer identifier</param>
        /// <param name="tenantUuid">The tenant identifier</param>
        /// <param name="isDraft">Whether is draft</param>
        /// <param name="key">The key to cache by</param>
        /// <param name="inMemoryCache">Whether to use static memory or per request/scope</param>
        /// <param name="expirationInSeconds">In memory cache expiration in seconds. Used when useCache is set to true</param>
        /// <param name="setFunc">Func to initialize the data if not found</param>
        /// <param name="version">The version of the record</param>
        /// <returns></returns>
        Task<TData> GetOrSetAsync<TData>(SemaphoreSlim semaphoreSlim, long developerUuid, long tenantUuid, bool isDraft, string key, bool inMemoryCache, double expirationInSeconds, Func<Task<TData>> setFunc, int? version = null) where TData : class;

        /// <summary>
        /// Clears the in-memory static cache instance
        /// </summary>
        /// <param name="developerUuid">The developer identifier</param>
        /// <param name="tenantUuid">The tenant identifier</param>
        /// <param name="isDraft">Whether is draft</param>
        void Clear(long developerUuid, long tenantUuid, bool isDraft);

        /// <summary>
        /// Removes data from the in-memory static cache instance
        /// </summary>
        /// <param name="developerUuid">The developer identifier</param>
        /// <param name="tenantUuid">The tenant identifier</param>
        /// <param name="isDraft">Whether is draft</param>
        /// <param name="key">The key to remove from cache by</param>
        void Remove(long developerUuid, long tenantUuid, bool isDraft, string key);
    }
}
