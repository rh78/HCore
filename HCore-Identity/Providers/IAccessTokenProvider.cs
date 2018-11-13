using HCore.Identity.Database.SqlServer.Models.Impl;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HCore.Identity.Providers
{
    public interface IAccessTokenProvider
    {
        Task<string> GetAccessTokenAsync(string userUuid, List<Claim> additionalClientClaims = null);
        Task<string> GetAccessTokenAsync(UserModel user, List<Claim> additionalClientClaims = null);
    }
}
