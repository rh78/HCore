using Microsoft.AspNetCore.Http;

namespace ReinhardHolzner.Core.Exceptions
{
    public class ServiceUnavailableApiException : ApiException
    {
        private string _errorCode;

        public const string ContentProviderMediaTypeUnknown = "content_provider_media_type_unknown";
        public const string ContentProviderNoAccessToken = "content_provider_no_access_token";
        public const string ContentProviderCompatibilityIssue = "content_provider_compatibility_issue";
        public const string ContentProviderForbidden = "content_provider_forbidden";
        public const string ContentProviderUnauthorized = "content_provider_unauthorized";
        public const string ContentProviderError = "content_provider_error";

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
