using Microsoft.AspNetCore.Http;

namespace ReinhardHolzner.Core.Exceptions
{
    public class AuthorizationFailedApiException : ApiException
    {
        private readonly string _errorCode;

        public const string AccessTokenExpired = "access_token_expired";

        public AuthorizationFailedApiException(string errorCode, string message) : 
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
