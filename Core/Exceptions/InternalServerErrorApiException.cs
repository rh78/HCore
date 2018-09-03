using Microsoft.AspNetCore.Http;
using System;

namespace ReinhardHolzner.Core.Exceptions
{
    public class InternalServerErrorApiException : ApiException
    {
        public InternalServerErrorApiException()
            : base("Unexpected server error")
        {

        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status500InternalServerError;
        }

        public override string GetErrorCode()
        {
            return "internalServerError";
        }        
    }
}
