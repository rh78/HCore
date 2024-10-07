using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace HCore.Identity.Requirements
{
    public class OemAdminRequirementHandler : AuthorizationHandler<OemAdminRequirement>
    {
        protected readonly IHttpContextAccessor HttpContextAccessor;

        public OemAdminRequirementHandler(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OemAdminRequirement requirement)
        {
            var oemAdminClaim = context.User.Claims.FirstOrDefault(c => c.Type == IdentityCoreConstants.OemAdminClaim);

            if (oemAdminClaim == null || string.IsNullOrEmpty(oemAdminClaim.Value))
            {
                oemAdminClaim = context.User.Claims.FirstOrDefault(c => c.Type == IdentityCoreConstants.OemAdminClientClaim);

                if (oemAdminClaim == null || string.IsNullOrEmpty(oemAdminClaim.Value))
                {
                    context.Fail();

                    return Task.FromResult(0);
                }
            }                

            string developerUuidString = oemAdminClaim.Value;

            long developerUuid;

            if (!long.TryParse(developerUuidString, out developerUuid))
            {
                context.Fail();

                return Task.FromResult(0);
            }

            HttpContext httpContext = HttpContextAccessor.HttpContext;

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
