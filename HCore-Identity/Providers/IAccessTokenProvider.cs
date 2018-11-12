using HCore.Identity.Database.SqlServer.Models.Impl;
using System.Threading.Tasks;

namespace HCore.Identity.Providers
{
    public interface IAccessTokenProvider
    {
        Task<string> GetAccessTokenAsync(string userUuid);
        Task<string> GetAccessTokenAsync(UserModel user);
    }
}
