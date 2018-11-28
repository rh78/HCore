using Microsoft.AspNetCore.Authorization;

namespace HCore.Identity.Requirements
{
    public class DeveloperAdminRequirement : IAuthorizationRequirement
    {
        public DeveloperAdminRequirement()
        {
        }
    }
}
