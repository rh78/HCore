using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace HCore.Identity.Requirements
{
    public class DeveloperAdminRequirementHandler : AuthorizationHandler<DeveloperAdminRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DeveloperAdminRequirementHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DeveloperAdminRequirement requirement)
        {
            var developerAdminClaim = context.User.Claims.FirstOrDefault(c => c.Type == IdentityCoreConstants.DeveloperAdminClaim);

            if (developerAdminClaim == null || string.IsNullOrEmpty(developerAdminClaim.Value))
            {
                context.Fail();

                return Task.FromResult(0);
            }

            string developerUuidString = developerAdminClaim.Value;

            long developerUuid;

            if (!long.TryParse(developerUuidString, out developerUuid))
            {
                context.Fail();

                return Task.FromResult(0);
            }

            HttpContext httpContext = _httpContextAccessor.HttpContext;

            var tenantInfo = httpContext.GetTenantInfo();
            
            if (tenantInfo == null || tenantInfo.DeveloperUuid != developerUuid)
            {
                context.Fail();

                return Task.FromResult(0);
            }

            context.Succeed(requirement);

            return Task.FromResult(0);
        }
    }
}
