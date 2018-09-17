using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReinhardHolzner.Core.Exceptions
{
    public class AuthorizationFailedApiException : ApiException
    {
        private string _errorCode;

        public const string AccessDeniedByUser = "access_denied_by_user";
        public const string AccessDeniedExternalReason = "access_denied_external_reason";
        public const string AuthorizationAlreadyProcessed = "authorization_already_processed";

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
