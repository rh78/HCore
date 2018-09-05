using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ReinhardHolzner.Core.Exceptions
{
    public abstract class ApiException : Exception
    {       
        public ApiException(string message)
            : base(message)
        {

        }

        public abstract int GetStatusCode();
        public abstract string GetErrorCode();
        public abstract object GetObject();

        internal async Task WriteResponseAsync(HttpContext context)
        {
            context.Response.StatusCode = GetStatusCode();

            await context.Response.WriteAsync(SerializeException());
        }

        public string SerializeException()
        {           
            Models.ApiException apiExceptionResult = new Models.ApiException()
            {
                ErrorCode = GetErrorCode(),
                ErrorMessage = Message
            };

            object o = GetObject();
            if (o != null)
            {
                apiExceptionResult.Details = JsonConvert.SerializeObject(o);
            }

            return JsonConvert.SerializeObject(apiExceptionResult, Formatting.None,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
