using System.Threading.Tasks;

namespace HCore.Identity.Providers
{
    public interface IAccessTokenProvider
    {
        Task<string> GetAccessTokenAsync(string userUuid);
    }
}
