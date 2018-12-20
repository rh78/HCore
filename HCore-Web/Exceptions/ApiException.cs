using System;
using System.Threading.Tasks;
using HCore.Web.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace HCore.Web.Exceptions
{
    public abstract class ApiException : Exception
    {
        private string _uuid;
        private string _name;

        public ApiException(string message)
            : base(message)
        {

        }

        public ApiException(string message, string name)
           : base(message)
        {
            _name = name;
        }

        public ApiException(string message, string uuid, string name)
           : base(message)
        {
            _uuid = uuid;
            _name = name;
        }

        public ApiException(string message, long uuid, string name)
          : base(message)
        {
            _uuid = Convert.ToString(uuid);
            _name = name;
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
            return JsonConvert.SerializeObject(GetErrorDetails(), Formatting.None,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        private string SerializeException()
        {           
            return JsonConvert.SerializeObject(GetApiExceptionModel(), Formatting.None,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        private ErrorDetails GetErrorDetails()
        {
            return new ErrorDetails()
            {
                Uuid = _uuid,
                Name = _name
            };
        }
    }
}
