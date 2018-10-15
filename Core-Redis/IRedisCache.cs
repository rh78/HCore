using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Redis
{
    public interface IRedisCache
    {
        Task StoreAsync(string key, int[] values);
        Task<int[]> GetIntArrayAsync(string key);

        Task StoreAsync(string key, long[] values);
        Task<long[]> GetLongArrayAsync(string key);
    }
}
