using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReinhardHolzner.Core.Exceptions
{
    public class NotFoundApiException : ApiException
    {
        private string _errorCode;

        public const string AccountConfigurationNotFound = "account_configuration_not_found";
        public const string ContractNotFound = "contract_not_found";
        public const string OfferingNotFound = "offering_not_found";
        public const string NotFound = "not_found";

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
