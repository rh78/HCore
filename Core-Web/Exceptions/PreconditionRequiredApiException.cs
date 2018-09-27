using Microsoft.AspNetCore.Http;

namespace ReinhardHolzner.Core.Web.Exceptions
{
    public class PreconditionRequiredApiException : ApiException
    {
        private readonly string _errorCode;

        public PreconditionRequiredApiException(string errorCode, string message) : 
            base(message)
        {
            _errorCode = errorCode;            
        }
       
        public override int GetStatusCode()
        {
            return StatusCodes.Status428PreconditionRequired;
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
