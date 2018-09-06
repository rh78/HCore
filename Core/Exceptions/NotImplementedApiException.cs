using Microsoft.AspNetCore.Http;
using System;

namespace ReinhardHolzner.Core.Exceptions
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
            return "notImplemented";
        }

        public override object GetObject()
        {
            return null;
        }
    }
}
