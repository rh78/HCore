using Microsoft.AspNetCore.Http;

namespace HCore.Web.Exceptions
{
    public class NotFoundApiException : ApiException
    {
        private readonly string _errorCode;

        public const string NotFound = "not_found";
        public const string UserNotFound = "user_not_found";
        public const string UserGroupNotFound = "user_group_not_found";
        public const string UserNoMemberOfUserGroup = "user_no_member_of_user_group";
        public const string DeveloperNotFound = "developer_not_found";
        public const string TenantNotFound = "tenant_not_found";

        public NotFoundApiException(string errorCode, string message) :
            base(message)
        {
            _errorCode = errorCode;
        }

        public NotFoundApiException(string errorCode, string message, string uuid) : 
            base(message, uuid, null)
        {
            _errorCode = errorCode;            
        }

        public NotFoundApiException(string errorCode, string message, long? uuid) :
           base(message, uuid, null)
        {
            _errorCode = errorCode;
        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status404NotFound;
        }

        public override string GetErrorCode()
        {
            return _errorCode;
        }
    }
}
