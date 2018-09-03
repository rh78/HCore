using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ReinhardHolzner.HCore.Exceptions
{
    public abstract class ApiException : Exception
    {       
        public ApiException(string message)
            : base(message)
        {

        }

        public abstract int GetStatusCode();
        public abstract string GetErrorCode();

        internal async Task WriteResponseAsync(HttpContext context)
        {
            context.Response.StatusCode = GetStatusCode();

            Models.ApiException apiExceptionResult = new Models.ApiException()
            {
                ErrorCode = GetErrorCode(),
                ErrorMessage = Message
            };

            await context.Response.WriteAsync(
                JsonConvert.SerializeObject(apiExceptionResult));
        }
    }
}
