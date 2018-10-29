using Microsoft.AspNetCore.Authorization;

namespace HCore.Identity.Requirements
{
    internal class ClientDeveloperUuidRequirement : IAuthorizationRequirement
    {
        public ClientDeveloperUuidRequirement()
        {
        }
    }
}
