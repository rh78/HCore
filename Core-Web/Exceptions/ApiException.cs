using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ReinhardHolzner.Core.Web.Exceptions
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

            await context.Response.WriteAsync(SerializeException()).ConfigureAwait(false);
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
                apiExceptionResult.ErrorDetails = JsonConvert.SerializeObject(o);
            }

            return JsonConvert.SerializeObject(apiExceptionResult, Formatting.None,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
