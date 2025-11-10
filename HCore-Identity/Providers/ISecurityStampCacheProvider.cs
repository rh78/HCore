using System.Threading.Tasks;

namespace HCore.Identity.Providers
{
    public interface ISecurityStampCacheProvider
    {
        Task<string> GetSecurityStampAsync(string userId);

        Task CreateOrUpdateSecurityStampAsync(string userId, string securityStamp);

        Task InvalidateSecurityStampAsync(string userId);
    }
}
