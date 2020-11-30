using HCore.Tenants.Models;
using HCore.Tenants.Providers;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;

namespace HCore.Identity.Services.Impl
{
    public class AuthServicesImpl : IAuthServices
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

            bool isAnonymous = IsAnonymous(httpContext, tenantInfo);
            bool isDeveloperAdmin = !isAnonymous && IsDeveloperAdmin(httpContext, tenantInfo);

            return new AuthInfoImpl()
            {
                UserUuid = userUuid,
                TenantInfo = tenantInfo,
                IsDeveloperAdmin = isDeveloperAdmin,
                IsAnonymous = isAnonymous
            };
        }

        private bool IsAnonymous(HttpContext context, ITenantInfo tenantInfo)
        {
            var anonymousUserClaim = context.User.Claims.FirstOrDefault(c => c.Type == IdentityCoreConstants.AnonymousUserClaim);

            if (anonymousUserClaim == null || string.IsNullOrEmpty(anonymousUserClaim.Value))
            {
                anonymousUserClaim = context.User.Claims.FirstOrDefault(c => c.Type == IdentityCoreConstants.AnonymousUserClientClaim);

                if (anonymousUserClaim == null || string.IsNullOrEmpty(anonymousUserClaim.Value))
                {
                    return false;
                }
            }

            string anonymousUserString = anonymousUserClaim.Value;

            long anonymousUserUuid;

            if (!long.TryParse(anonymousUserString, out anonymousUserUuid))
            {
                return false;
            }

            if (tenantInfo.DeveloperUuid != anonymousUserUuid)
            {
                return false;
            }

            return true;
        }

        private bool IsDeveloperAdmin(HttpContext context, ITenantInfo tenantInfo)
        {
            var developerAdminClaim = context.User.Claims.FirstOrDefault(c => c.Type == IdentityCoreConstants.DeveloperAdminClaim);

            if (developerAdminClaim == null || string.IsNullOrEmpty(developerAdminClaim.Value))
            {
                developerAdminClaim = context.User.Claims.FirstOrDefault(c => c.Type == IdentityCoreConstants.DeveloperAdminClientClaim);

                if (developerAdminClaim == null || string.IsNullOrEmpty(developerAdminClaim.Value))
                {
                    return false;
                }
            }

            string developerAdminString = developerAdminClaim.Value;

            long developerAdminUuid;

            if (!long.TryParse(developerAdminString, out developerAdminUuid))
            {
                return false;
            }

            if (tenantInfo.DeveloperUuid != developerAdminUuid)
            {
                return false;
            }

            return true;
        }
    }
}
