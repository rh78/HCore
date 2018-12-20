using Microsoft.AspNetCore.Http;
using System;

namespace HCore.Web.Exceptions
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
            return "internal_server_error";
        }
    }
}
