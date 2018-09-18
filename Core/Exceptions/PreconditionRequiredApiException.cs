using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReinhardHolzner.Core.Exceptions
{
    public class PreconditionRequiredApiException : ApiException
    {
        private string _errorCode;

        public const string AccountConfigurationNotSetup = "account_configuration_not_setup";
        public const string AccountConfigurationNotReady = "account_configuration_not_ready";

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
