using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HCore.Web.Exceptions;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HCore.Web.Middleware
{
    internal class UnhandledExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UnhandledExceptionHandlingMiddleware> _logger;

        public UnhandledExceptionHandlingMiddleware(RequestDelegate next, ILogger<UnhandledExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(HttpContext context)
        {
            ApiException resultException = null;

            try
            {
                await _next.Invoke(context).ConfigureAwait(false);
            }
            catch (ApiException e)
            {
                resultException = e;
            }
            catch (JsonSerializationException e)
            {
                resultException = new RequestFailedApiException(RequestFailedApiException.ArgumentInvalid, e.Message);
            }
            catch (NotImplementedException e)
            {
                _logger.LogError($"Not implemented exception: {e}");

                resultException = new NotImplementedApiException();
            }
            catch (Exception e)
            {
                if (!string.IsNullOrEmpty(e.Message) && e.Message.Contains("IDX20803"))
                {
                    resultException = new ServiceUnavailableApiException(ServiceUnavailableApiException.AuthorizationAuthorityNotAvailable, "The authorization authority for this service is currently not available. Your access credentials cannot be validated. Please try again later");
                }
                else
                {
                    _logger.LogError($"Unexpected server error: {e}");

                    resultException = new InternalServerErrorApiException();
                }
            }            

            if (resultException != null)            
                await resultException.WriteResponseAsync(context).ConfigureAwait(false);            
        }
    }
}
