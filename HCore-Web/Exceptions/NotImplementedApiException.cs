using Microsoft.AspNetCore.Http;
using System;

namespace HCore.Web.Exceptions
{
    public class NotImplementedApiException : ApiException
    {
        public NotImplementedApiException()
            : base("This functionality is not yet implemented")
        {

        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status501NotImplemented;
        }

        public override string GetErrorCode()
        {
            return "not_implemented";
        }
    }
}
