using System;
using System.Globalization;
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

        public ApiException(string message, DateTimeOffset? dateTimeOffset)
           : base(message)
        {
            Uuid = null;
            Name = dateTimeOffset?.ToString("g", CultureInfo.CurrentCulture);            
        }

        public ApiException(string message, string uuid, string name)
           : base(message)
        {
            Uuid = uuid;
            Name = name;
        }

        public ApiException(string message, long? uuid, string name)
          : base(message)
        {
            Uuid = uuid != null ? Convert.ToString(uuid) : null;
            Name = name;
        }

        public abstract int GetStatusCode();
        public abstract string GetErrorCode();
        
        public async virtual Task WriteResponseAsync(HttpContext context, string redirectUrl = null)
        {
            context.Response.StatusCode = GetStatusCode();
            context.Response.ContentType = System.Net.Mime.MediaTypeNames.Application.Json;

            await context.Response.WriteAsync(SerializeException(redirectUrl)).ConfigureAwait(false);            
        }

        public Models.ApiException GetApiExceptionModel(string redirectUrl = null)
        {
            Models.ApiException apiExceptionResult = new Models.ApiException()
            {
                ErrorCode = GetErrorCode(),
                ErrorMessage = Message
            };

            apiExceptionResult.ErrorDetails = GetErrorDetails();

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                apiExceptionResult.RedirectUrl = redirectUrl;
            }
            
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

        protected virtual string SerializeException(string redirectUrl = null)
        {           
            return JsonConvert.SerializeObject(GetApiExceptionModel(redirectUrl), Formatting.None,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        protected ErrorDetails GetErrorDetails()
        {
            if (string.IsNullOrEmpty(Uuid) && string.IsNullOrEmpty(Name))
                return null;

            return new ErrorDetails()
            {
                Uuid = Uuid,
                Name = Name
            };
        }

        public virtual bool Redirect()
        {
            return false;
        }
    }
}
