using Microsoft.AspNetCore.Http;

namespace HCore.Web.Exceptions
{
    public class ServiceUnavailableApiException : ApiException
    {
        private readonly string _errorCode;

        public const string AuthorizationAuthorityNotAvailable = "authorization_authority_not_available";
        public const string BackendServiceNotAvailable = "backend_service_not_available";

        public ServiceUnavailableApiException(string errorCode, string message, string name) : 
            base(message, name)
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
    }
}
