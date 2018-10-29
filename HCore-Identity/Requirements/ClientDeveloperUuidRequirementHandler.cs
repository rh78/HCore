using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace HCore.Identity.Requirements
{
    internal class ClientDeveloperUuidRequirementHandler : AuthorizationHandler<ClientDeveloperUuidRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientDeveloperUuidRequirementHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ClientDeveloperUuidRequirement requirement)
        {
            var developerUuidClaim = context.User.Claims.FirstOrDefault(c => c.Type == IdentityCoreConstants.DeveloperUuidClientClaim);

            if (developerUuidClaim == null || string.IsNullOrEmpty(developerUuidClaim.Value))
            {
                context.Fail();

                return Task.FromResult(0);
            }

            string developerUuidString = developerUuidClaim.Value;

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
