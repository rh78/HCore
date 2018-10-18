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
    }
}