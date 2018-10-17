using System.Threading.Tasks;

namespace HCore.Redis
{
    public interface IRedisCache
    {
        Task StoreAsync(string key, object value);
        Task<T> GetAsync<T>(string key) where T : class;        
    }
}
