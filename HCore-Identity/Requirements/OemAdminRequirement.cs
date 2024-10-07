using Microsoft.AspNetCore.Authorization;

namespace HCore.Identity.Requirements
{
    public class OemAdminRequirement : IAuthorizationRequirement
    {
        public OemAdminRequirement()
        {
        }
    }
}
