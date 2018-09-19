using Microsoft.AspNetCore.Http;

namespace ReinhardHolzner.Core.Exceptions
{
    public class OptimisticLockingApiException : ApiException
    {
        private string _errorCode;

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
