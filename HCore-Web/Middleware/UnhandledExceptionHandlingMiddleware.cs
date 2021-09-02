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
using Microsoft.AspNetCore.DataProtection;
using System.Linq;

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
        private string _tenantSelectorFallbackUrl;

        private readonly bool _blockIE;

        private readonly IDataProtectionProvider _dataProtectionProvider;

        public UnhandledExceptionHandlingMiddleware(
            RequestDelegate next,
            IDataProtectionProvider dataProtectionProvider,
            IServiceProvider serviceProvider, 
            IConfiguration configuration, 
            ILogger<UnhandledExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            bool useWeb = configuration.GetValue<bool>("WebServer:UseWeb");
            bool useApi = configuration.GetValue<bool>("WebServer:UseApi");

            _blockIE = configuration.GetValue<bool>("WebServer:BlockIE");

            _criticalFallbackUrl = configuration["WebServer:CriticalFallbackUrl"];
            _tenantSelectorFallbackUrl = configuration["Identity:Tenants:TenantSelectorFallbackUrl"];

            if (string.IsNullOrEmpty(_criticalFallbackUrl))
                throw new Exception("The critical fallback URL is not set");

            if (string.IsNullOrEmpty(_tenantSelectorFallbackUrl))
                _tenantSelectorFallbackUrl = null;

            if (useWeb)
            {
                _webPort = configuration.GetValue<int>("WebServer:WebPort");
            }

            if (useApi)
            {
                _apiPort = configuration.GetValue<int>("WebServer:ApiPort");
            }

            _translationsProvider = serviceProvider.GetService<ITranslationsProvider>();

            _dataProtectionProvider = dataProtectionProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            ApiException resultException = null;

            try
            {
                var blocked = BlockIE11(context);

                if (!blocked)
                {
                    await _next.Invoke(context).ConfigureAwait(false);
                }
            }
            catch (RedirectApiException e)
            {
                string path = context.Request.Path;

                if (!string.Equals(path, e.Location))
                {
                    context.Response.Redirect(e.Location);
                }

                return;
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
                    _logger.LogError($"Authorization authority is not available: {e}");

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

        private bool BlockIE11(HttpContext context)
        {
            if (context == null)
                return false;

            if (!_blockIE)
                return false;

            if (_webPort == null || context.Connection.LocalPort != _webPort)
            {
                // we have a call to some API endpoint, that's OK

                return false;
            }

            if (!context.Request.Headers.ContainsKey("User-Agent"))
                return false;

            var path = context.Request.Path.Value;

            if (!string.IsNullOrEmpty(path) && (path.ToLower().StartsWith("/error") || path.Contains("/js/") || path.Contains("/css/") || path.Contains("/fonts/")))
            {
                return false;
            }

            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(userAgent))
                return false;

            if (userAgent.Contains("MSIE ") ||
                userAgent.Contains("Trident/") || true)
            {
                // IE < 11, IE 11 or similar

                var redirectUrl = $"/Error?errorCode=ie11_and_lower_not_supported";

                context.Response.Redirect(redirectUrl);

                return true;
            }

            // lets allow Edge, it should be fine

            return false;
        }

        private async Task HandleResultExceptionAsync(HttpContext context, ApiException resultException)
        {
            string path = context.Request.Path;

            if (string.Equals(resultException.GetErrorCode(), NotFoundApiException.TenantNotFound) &&
                _tenantSelectorFallbackUrl != null &&
                !string.Equals(path, _tenantSelectorFallbackUrl))
            {
                // redirect to the tenant selector

                context.Response.Redirect(_tenantSelectorFallbackUrl);

                return;
            }

            string redirectUrl = null;

            if (_webPort == null || context.Connection.LocalPort != _webPort)
            { 
                // we have a call to some API endpoint, so just return the error JSON

                if (resultException.Redirect() && (string.IsNullOrEmpty(path) || !path.ToLower().StartsWith("/error")))
                {
                    redirectUrl = GetRedirectUrl(resultException);
                }

                await resultException.WriteResponseAsync(context, redirectUrl).ConfigureAwait(false);

                return;
            }

            // we have a call to our web interface, we need to go to the error page
            // but not twice, because then we'd run in circles

            if (!string.IsNullOrEmpty(path) && path.ToLower().StartsWith("/error"))
            {
                // we ARE already on the error page, use critical fallback URL

                context.Response.Redirect(_criticalFallbackUrl);

                return;
            }

            redirectUrl = GetRedirectUrl(resultException);

            context.Response.Redirect(redirectUrl);
        }

        private string GetRedirectUrl(ApiException resultException)
        {
            string errorCode = resultException.GetErrorCode();
            string errorDescription = resultException.Message;

            if (_translationsProvider != null)
                errorDescription = _translationsProvider.TranslateError(resultException.GetErrorCode(), resultException.Message, resultException.Uuid, resultException.Name);

            if (!string.IsNullOrEmpty(errorDescription))
                errorDescription = _dataProtectionProvider.CreateProtector("Error").Protect(errorDescription);

            return $"/Error?errorCode={HttpUtility.UrlEncode(errorCode ?? "")}&errorDescription={HttpUtility.UrlEncode(errorDescription ?? "")}";
        }
    }
}
