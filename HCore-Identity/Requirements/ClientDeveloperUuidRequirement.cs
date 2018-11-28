using Microsoft.AspNetCore.Authorization;

namespace HCore.Identity.Requirements
{
    public class ClientDeveloperUuidRequirement : IAuthorizationRequirement
    {
        public ClientDeveloperUuidRequirement()
        {
        }
    }
}
