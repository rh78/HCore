using Microsoft.AspNetCore.Http;

namespace HCore.Web.Exceptions
{
    public class UnauthorizedApiException : ApiException
    {
        private readonly string _errorCode;

        public const string AccessTokenExpired = "access_token_expired";
        public const string AccountLockedOut = "account_locked_out";
        public const string EmailNotConfirmed = "email_not_confirmed";
        public const string InvalidCredentials = "invalid_credentials";
        public const string PasswordDoesNotMatch = "password_does_not_match";
        public const string TokenInvalidOrExpired = "token_invalid_or_expired";
        public const string CookieInvalidOrExpired = "cookie_invalid_or_expired";
        public const string ExternalUserIdIsMissing = "external_user_id_is_missing";

        public string UserUuid { get; set; }

        public UnauthorizedApiException(string errorCode, string message) : 
            base(message)
        {
            _errorCode = errorCode;            
        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status401Unauthorized;
        }

        public override string GetErrorCode()
        {
            return _errorCode;
        }        
    }
}
