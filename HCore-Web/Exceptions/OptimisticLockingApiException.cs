using Microsoft.AspNetCore.Http;

namespace HCore.Web.Exceptions
{
    public class OptimisticLockingApiException : ApiException
    {
        private readonly string _errorCode;

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
    }
}
