using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReinhardHolzner.Core.Exceptions
{
    public class InvalidArgumentApiException : ApiException
    {
        private string _errorCode;
        private ModelStateDictionary _modelState;

        public const string InvalidArgument = "invalidArgument";
        public const string MandateUuidInvalid = "mandateUuidInvalid";
        public const string CustomerUuidInvalid = "customerUuidInvalid";
        public const string MaxPagingOffsetExceeded = "maxPagingOffsetExceeded";
        public const string MaxPagingLimitExceeded = "maxPagingLimitExceeded";
        public const string PagingOffsetInvalid = "pagingOffsetInvalid";
        public const string PagingLimitInvalid = "pagingLimitInvalid";

        public InvalidArgumentApiException(string errorCode, string message) : 
            base(message)
        {
            _errorCode = errorCode;            
        }

        public InvalidArgumentApiException(string errorCode, string message, ModelStateDictionary modelState) :
           this(errorCode, message)
        {
            _modelState = modelState;
        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status400BadRequest;
        }

        public override string GetErrorCode()
        {
            return _errorCode;
        }

        public override object GetObject()
        {
            return _modelState;
        }
    }
}
