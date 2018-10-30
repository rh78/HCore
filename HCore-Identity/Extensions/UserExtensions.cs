using System.Linq;

namespace System.Security.Claims
{
    public static class UserExtensions
    {        
        public static string GetEmailAddress(this ClaimsPrincipal user)
        {
            var emailAddressClaim = user.Claims.FirstOrDefault(claim => claim.Type == "email" || claim.Type == ClaimTypes.Email);

            string emailAddress = emailAddressClaim?.Value;

            if (string.IsNullOrEmpty(emailAddress))
                return null;

            return emailAddress;
        }    

        public static string GetUserUuid(this ClaimsPrincipal user)
        {            
            var userUuidClaim = user.Claims.FirstOrDefault(claim => claim.Type == "sub" || claim.Type == ClaimTypes.NameIdentifier);

            string userUuid = userUuidClaim?.Value;

            if (string.IsNullOrEmpty(userUuid))
                return null;

            return userUuid;
        }
    }
}