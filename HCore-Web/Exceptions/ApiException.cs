using System;
using System.Threading.Tasks;
using HCore.Web.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace HCore.Web.Exceptions
{
    public abstract class ApiException : Exception
    {
        public string Uuid { get; private set; }
        public string Name { get; private set; }

        public ApiException(string message)
            : base(message)
        {

        }

        public ApiException(string message, string name)
           : base(message)
        {
            Name = name;
        }

        public ApiException(string message, string uuid, string name)
           : base(message)
        {
            Uuid = uuid;
            Name = name;
        }

        public ApiException(string message, long uuid, string name)
          : base(message)
        {
            Uuid = Convert.ToString(uuid);
            Name = name;
        }

        public abstract int GetStatusCode();
        public abstract string GetErrorCode();
        
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

            apiExceptionResult.ErrorDetails = GetErrorDetails();
            
            return apiExceptionResult;
        }

        public string SerializeErrorDetails()
        {
            var errorDetails = GetErrorDetails();

            if (errorDetails == null)
                return null;

            return JsonConvert.SerializeObject(errorDetails, Formatting.None,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        private string SerializeException()
        {           
            return JsonConvert.SerializeObject(GetApiExceptionModel(), Formatting.None,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        private ErrorDetails GetErrorDetails()
        {
            if (string.IsNullOrEmpty(Uuid) && string.IsNullOrEmpty(Name))
                return null;

            return new ErrorDetails()
            {
                Uuid = Uuid,
                Name = Name
            };
        }
    }
}
