using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReinhardHolzner.Core.Exceptions
{
    public class OptimisticLockingApiException : ApiException
    {
        private string _errorCode;

        public const string AccountConfigurationOptimisticLockViolated = "account_configuration_optimistic_lock_violated";        

        public OptimisticLockingApiException(string errorCode, string message) : 
            base(message)
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

        public override object GetObject()
        {
            return null;
        }
    }
}
