using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Cache.Impl
{
    public class MemoryScopedCacheImpl : IMemoryScopedCache
    {
        private readonly static ConcurrentDictionary<string, ConcurrentDictionary<string, MemoryScopedCacheWrapperModel>> _inMemoryComponentCacheByKey = new ConcurrentDictionary<string, ConcurrentDictionary<string, MemoryScopedCacheWrapperModel>>();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MemoryScopedCacheWrapperModel>> _outOfMemoryComponentCacheByKey = new ConcurrentDictionary<string, ConcurrentDictionary<string, MemoryScopedCacheWrapperModel>>();

        public async Task<TData> GetOrSetAsync<TData>(SemaphoreSlim semaphoreSlim, long developerUuid, long tenantUuid, bool isDraft, string key, double expirationInSeconds, Func<Task<TData>> typedFunc, int? version = null) where TData : class
        {
            return await GetOrSetAsync(semaphoreSlim, developerUuid, tenantUuid, isDraft, key, inMemoryCache: true, expirationInSeconds, typedFunc, version).ConfigureAwait(false);
        }

        public async Task<TData> GetOrSetAsync<TData>(SemaphoreSlim semaphoreSlim, long developerUuid, long tenantUuid, bool isDraft, string key, bool inMemoryCache, double expirationInSeconds, Func<Task<TData>> setFunc, int? version = null) where TData : class
        {
            if (semaphoreSlim == null)
            {
                throw new ArgumentNullException(nameof(semaphoreSlim));
            }

            if (string.IsNullOrEmpty(key))
            {
                return default;
            }

            TData data;

            if (inMemoryCache)
            {
                // Static / long lived cache (in memory)
                data = await GetCacheWrapperDataAsync(
                    semaphoreSlim,
                    _inMemoryComponentCacheByKey,
                    developerUuid,
                    tenantUuid,
                    isDraft,
                    key,
                    key => GetInMemoryScopedCacheWrapperModel(developerUuid, tenantUuid, isDraft, key, expirationInSeconds),
                    setFunc,
                    version).ConfigureAwait(false);
            }
            else
            {
                // Request / scoped cache (out of memory)
                data = await GetCacheWrapperDataAsync(
                    semaphoreSlim,
                    _outOfMemoryComponentCacheByKey,
                    developerUuid,
                    tenantUuid,
                    isDraft,
                    key,
                    key => GetOutMemoryScopedCacheWrapperModel(developerUuid, tenantUuid, isDraft, key),
                    setFunc,
                    version).ConfigureAwait(false);
            }

            return data;
        }

        private static async Task<TData> GetCacheWrapperDataAsync<TData>(
            SemaphoreSlim semaphoreSlim,
            ConcurrentDictionary<string, ConcurrentDictionary<string, MemoryScopedCacheWrapperModel>> componentCacheByKey,
            long developerUuid,
            long tenantUuid,
            bool isDraft,
            string key,
            Func<string, MemoryScopedCacheWrapperModel> cacheWrapperFunc,
            Func<Task<TData>> setFunc,
            int? version) where TData : class
        {
            var memoryScopedCacheWrapperModel = cacheWrapperFunc(key);

            if (memoryScopedCacheWrapperModel != null)
            {
                if (version != null)
                {
                    var existingVersion = memoryScopedCacheWrapperModel.Version;

                    if (existingVersion != null && existingVersion.Value >= version.Value)
                    {
                        // the existing version is equal or newer than the requested value
                        // take existing record

                        return memoryScopedCacheWrapperModel.Data as TData;
                    }

                    // otherwise refresh the cache
                }
                else
                {
                    // no version requirement at all

                    return memoryScopedCacheWrapperModel.Data as TData;
                }
            }

            await semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                memoryScopedCacheWrapperModel = cacheWrapperFunc(key);

                if (memoryScopedCacheWrapperModel != null)
                {
                    return memoryScopedCacheWrapperModel.Data as TData;
                }

                if (setFunc == null)
                {
                    return default;
                }

                var data = await setFunc().ConfigureAwait(false);

                var componentKey = GetComponentKey(developerUuid, tenantUuid, isDraft);

                if (!componentCacheByKey.TryGetValue(componentKey, out var memoryScopedCacheWrapperModelByKey))
                {
                    memoryScopedCacheWrapperModelByKey = new ConcurrentDictionary<string, MemoryScopedCacheWrapperModel>();
                }

                memoryScopedCacheWrapperModel = new MemoryScopedCacheWrapperModel
                {
                    Data = data,
                    Version = version,
                    CreatedAt = DateTime.UtcNow
                };

                memoryScopedCacheWrapperModelByKey.AddOrUpdate(
                    key,
                    memoryScopedCacheWrapperModel,
                    (key, oldValue) => memoryScopedCacheWrapperModel);

                componentCacheByKey.AddOrUpdate(
                    componentKey,
                    memoryScopedCacheWrapperModelByKey,
                    (key, oldValue) => memoryScopedCacheWrapperModelByKey);

                return data;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private static string GetComponentKey(long developerUuid, long tenantUuid, bool isDraft)
        {
            return $"{developerUuid}_{tenantUuid}_{isDraft}";
        }

        private static MemoryScopedCacheWrapperModel GetInMemoryScopedCacheWrapperModel(long developerUuid, long tenantUuid, bool isDraft, string key, double expirationInSeconds)
        {
            var componentKey = GetComponentKey(developerUuid, tenantUuid, isDraft);

            if (!_inMemoryComponentCacheByKey.TryGetValue(componentKey, out var memoryScopedCacheWrapperModelsByKey))
            {
                return null;
            }

            if (!memoryScopedCacheWrapperModelsByKey.TryGetValue(key, out var memoryScopedCacheWrapperModel))
            {
                return null;
            }

            // we will just cache very short to avoid load peaks, but to still give some kind of "real time" feeling

            if (DateTime.UtcNow - memoryScopedCacheWrapperModel.CreatedAt > TimeSpan.FromSeconds(expirationInSeconds))
            {
                return null;
            }

            return memoryScopedCacheWrapperModel;
        }

        private MemoryScopedCacheWrapperModel GetOutMemoryScopedCacheWrapperModel(long developerUuid, long tenantUuid, bool isDraft, string key)
        {
            var componentKey = GetComponentKey(developerUuid, tenantUuid, isDraft);

            if (!_outOfMemoryComponentCacheByKey.TryGetValue(componentKey, out var memoryScopedCacheWrapperModelsByKey))
            {
                return null;
            }

            if (!memoryScopedCacheWrapperModelsByKey.TryGetValue(key, out var memoryScopedCacheWrapperModel))
            {
                return null;
            }

            return memoryScopedCacheWrapperModel;
        }

        public void Clear(long developerUuid, long tenantUuid, bool isDraft)
        {
            var componentKey = GetComponentKey(developerUuid, tenantUuid, isDraft);

            _inMemoryComponentCacheByKey.TryRemove(componentKey, out var _);
        }

        public void Remove(long developerUuid, long tenantUuid, bool isDraft, string key)
        {
            var componentKey = GetComponentKey(developerUuid, tenantUuid, isDraft);

            if (!_inMemoryComponentCacheByKey.TryGetValue(componentKey, out var cacheByKey))
            {
                return;
            }

            cacheByKey.TryRemove(key, out var _);
        }

        private class MemoryScopedCacheWrapperModel
        {
            public object Data { get; set; }

            public int? Version { get; set; }

            public DateTime CreatedAt { get; set; }
        }
    }
}
