using Microsoft.AspNetCore.Http;

namespace ReinhardHolzner.Core.Exceptions
{
    public class ServiceUnavailableApiException : ApiException
    {
        private string _errorCode;

        public const string ContentProviderNoAccessToken = "contentProviderNoAccessToken";
        public const string ContentProviderCompatibilityIssue = "contentProviderCompatibilityIssue";
        public const string ContentProviderForbidden = "contentProviderForbidden";
        public const string ContentProviderUnauthorized = "contentProviderUnauthorized";
        public const string ContentProviderError = "contentProviderError";

        public ServiceUnavailableApiException(string errorCode, string message) : 
            base(message)
        {
            _errorCode = errorCode;            
        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status503ServiceUnavailable;
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
