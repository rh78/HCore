using Microsoft.AspNetCore.Http;

namespace ReinhardHolzner.Core.Web.Exceptions
{
    public class UnauthorizedApiException : ApiException
    {
        private readonly string _errorCode;

        public const string AccessTokenExpired = "access_token_expired";
        public const string AccountLockedOut = "account_locked_out";
        public const string InvalidCredentials = "invalid_credentials";
        public const string PasswordDoesNotMatch = "password_does_not_match";

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

        public override object GetObject()
        {
            return null;
        }
    }
}
