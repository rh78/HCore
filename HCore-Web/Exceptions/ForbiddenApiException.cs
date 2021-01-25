using Microsoft.AspNetCore.Http;

namespace HCore.Web.Exceptions
{
    public class ForbiddenApiException : ApiException
    {
        private readonly string _errorCode;

        public const string PermissionDenied = "permission_denied";
        public const string SelfRegistrationNotAllowed = "self_registration_not_allowed";
        public const string SelfServiceNotAllowed = "self_service_not_allowed";
        public const string RedirectNecessary = "redirect_necessary";

        public ForbiddenApiException(string errorCode, string message) : 
            base(message)
        {
            _errorCode = errorCode;            
        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status403Forbidden;
        }

        public override string GetErrorCode()
        {
            return _errorCode;
        }
    }
}
