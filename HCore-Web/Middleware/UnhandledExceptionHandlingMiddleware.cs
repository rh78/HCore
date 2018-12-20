using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HCore.Web.Exceptions;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Web;
using HCore.Translations.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace HCore.Web.Middleware
{
    internal class UnhandledExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ITranslationsProvider _translationsProvider;

        private readonly ILogger<UnhandledExceptionHandlingMiddleware> _logger;

        private int? _webPort;
        private int? _apiPort;

        private string _criticalFallbackUrl;

        public UnhandledExceptionHandlingMiddleware(RequestDelegate next, IServiceProvider serviceProvider, IConfiguration configuration, ILogger<UnhandledExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            bool useWeb = configuration.GetValue<bool>("WebServer:UseWeb");
            bool useApi = configuration.GetValue<bool>("WebServer:UseApi");

            _criticalFallbackUrl = configuration["WebServer:CriticalFallbackUrl"];

            if (string.IsNullOrEmpty(_criticalFallbackUrl))
                throw new Exception("The critical fallback URL is not set");

            if (useWeb)
            {
                _webPort = configuration.GetValue<int>("WebServer:WebPort");
            }

            if (useApi)
            {
                _apiPort = configuration.GetValue<int>("WebServer:ApiPort");
            }

            _translationsProvider = serviceProvider.GetService<ITranslationsProvider>();
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
            {
                await HandleResultExceptionAsync(context, resultException).ConfigureAwait(false);
            }                           
        }

        private async Task HandleResultExceptionAsync(HttpContext context, ApiException resultException)
        {
            if (_webPort == null || context.Connection.LocalPort != _webPort)
            {
                // we have a call to some endpoint outside of our web interface, so just return the error JSON

                await resultException.WriteResponseAsync(context).ConfigureAwait(false);

                return;
            }

            // we have a call to our web interface, we need to go to the error page
            // but not twice, because then we'd run in circles

            string path = context.Request.Path;
            if (!string.IsNullOrEmpty(path) && path.ToLower().StartsWith("/error"))
            {
                // we ARE already on the error page, use critical fallback URL

                context.Response.Redirect(_criticalFallbackUrl);

                return;
            }

            string errorCode = resultException.GetErrorCode();
            string errorDescription = resultException.Message;

            if (_translationsProvider != null)
            {
                string translatedMessage = _translationsProvider.GetString(errorCode);
                if (translatedMessage != null && !string.Equals(errorCode, translatedMessage))
                    errorDescription = translatedMessage;
            }

            string uuid = resultException.Uuid;

            if (!string.IsNullOrEmpty(uuid) && !string.IsNullOrEmpty(errorDescription))
                errorDescription = errorDescription.Replace("{uuid}", uuid);

            string name = resultException.Name;

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(errorDescription))
                errorDescription = errorDescription.Replace("{name}", name);

            context.Response.Redirect($"/Error?errorCode={HttpUtility.UrlEncode(errorCode ?? "")}&errorDescription={HttpUtility.UrlEncode(errorDescription ?? "")}");
        }
    }
}
