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
            context.Response.ContentType = System.Net.Mime.MediaTypeNames.Application.Json;

            await context.Response.WriteAsync(SerializeException()).ConfigureAwait(false);            
        }

        public Models.ApiException GetApiExceptionModel()
        {
            Models.ApiException apiExceptionResult = new Models.ApiException()
            {
                ErrorCode = GetErrorCode(),
                ErrorMessage = Message
            };

            apiExceptionResult.ErrorDetails = SerializeErrorDetails(GetObject());
            
            return apiExceptionResult;
        }

        private string SerializeException()
        {           
            return JsonConvert.SerializeObject(GetApiExceptionModel(), Formatting.None,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        private string SerializeErrorDetails(object o)
        {
            return o != null ? JsonConvert.SerializeObject(o) : null;
        }
    }
}
