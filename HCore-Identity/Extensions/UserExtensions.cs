using System.Linq;
using IdentityModel;

namespace System.Security.Claims
{
    public static class UserExtensions
    {        
        public static string GetEmailAddress(this ClaimsPrincipal user)
        {
            var emailAddressClaim = user.Claims.FirstOrDefault(claim => claim.Type == JwtClaimTypes.Email || claim.Type == ClaimTypes.Email);

            string emailAddress = emailAddressClaim?.Value;

            if (string.IsNullOrEmpty(emailAddress))
                return null;

            return emailAddress;
        }

        public static string GetUserUuid(this ClaimsPrincipal user)
        {
            var userUuidClaim = user.Claims.FirstOrDefault(claim => claim.Type == JwtClaimTypes.Subject || claim.Type == ClaimTypes.NameIdentifier);

            string userUuid = userUuidClaim?.Value;

            if (!string.IsNullOrEmpty(userUuid))
            {
                return userUuid;
            }

            var clientUserUuidClaim = user.Claims.FirstOrDefault(claim => claim.Type == "client_sub");

            userUuid = clientUserUuidClaim?.Value;

            if (!string.IsNullOrEmpty(userUuid))
            { 
                return userUuid;
            }

            return null;
        }
    }
}