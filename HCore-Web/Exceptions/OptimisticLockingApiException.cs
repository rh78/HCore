using Microsoft.AspNetCore.Http;

namespace HCore.Web.Exceptions
{
    public class OptimisticLockingApiException : ApiException
    {
        private readonly string _errorCode;

        public const string OptimisticLockViolated = "optimistic_lock_violated";
        public const string TenantOptimisticLockViolated = "tenant_optimistic_lock_violated";
        public const string UserGroupOptimisticLockViolated = "user_group_optimistic_lock_violated";
        public const string UserOptimisticLockViolated = "user_optimistic_lock_violated";

        public OptimisticLockingApiException(string errorCode, string message) :
            base(message)
        {
            _errorCode = errorCode;        
        }

        public OptimisticLockingApiException(string errorCode, string message, string uuid, string name) :
                    base(message, uuid, name)
        {
            _errorCode = errorCode;
        }

        public OptimisticLockingApiException(string errorCode, string message, long? uuid, string name) :
            base(message, uuid, name)
        {
            _errorCode = errorCode;
        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status409Conflict;
        }

        public override string GetErrorCode()
        {
            return _errorCode;
        }
    }
}
