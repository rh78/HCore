using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReinhardHolzner.Core.Exceptions
{
    public class NotFoundApiException : ApiException
    {
        private string _errorCode;

        public const string AccountConfigurationNotFound = "accountConfigurationNotFound";
        public const string ContractNotFound = "contractNotFound";
        public const string OfferingNotFound = "offeringNotFound";
        
        public NotFoundApiException(string errorCode, string message) : 
            base(message)
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

        public override object GetObject()
        {
            return null;
        }
    }
}
