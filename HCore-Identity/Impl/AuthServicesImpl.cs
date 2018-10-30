using HCore.Tenants;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace HCore.Identity.Impl
{
    internal class AuthServicesImpl : IAuthServices
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        public AuthServicesImpl(
            IHttpContextAccessor httpContextAccessor,
            ITenantInfoAccessor tenantInfoAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _tenantInfoAccessor = tenantInfoAccessor;
        }

        public IAuthInfo AuthInfo { get => GetAuthInfo(); }

        private IAuthInfo GetAuthInfo()
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;

            string userUuid = httpContext.User.GetUserUuid();

            ITenantInfo tenantInfo = _tenantInfoAccessor.TenantInfo;

            return new AuthInfoImpl()
            {
                UserUuid = userUuid,
                TenantInfo = tenantInfo
            };
        }
    }
}
