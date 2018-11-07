using System.Threading.Tasks;

namespace HCore.Identity
{
    public interface IAccessTokenProvider
    {
        Task<string> GetAccessTokenAsync(string userUuid);
    }
}
