using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReinhardHolzner.Core.Exceptions
{
    public class AuthorizationFailedApiException : ApiException
    {
        private string _errorCode;

        public const string AccessDeniedByUser = "accessDeniedByUser";
        public const string AccessDeniedExternalReason = "accessDeniedExternalReason";
        public const string AuthorizationAlreadyProcessed = "authorizationAlreadyProcessed";

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
