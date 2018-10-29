using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace HCore.Web.Exceptions
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

        public async Task WriteResponseAsync(HttpContext context)
        {
            context.Response.StatusCode = GetStatusCode();

            await context.Response.WriteAsync(SerializeException()).ConfigureAwait(false);
        }

        public Models.ApiException GetApiExceptionModel()
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

            return apiExceptionResult;
        }

        public string SerializeException()
        {           
            return JsonConvert.SerializeObject(GetApiExceptionModel(), Formatting.None,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
