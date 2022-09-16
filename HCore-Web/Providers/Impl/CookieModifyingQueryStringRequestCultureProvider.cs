using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace HCore.Web.Providers.Impl
{
    public class CookieModifyingQueryStringRequestCultureProvider : QueryStringRequestCultureProvider
    {
        private readonly string _cookieName;
        private readonly bool _httpOnly;

        public CookieModifyingQueryStringRequestCultureProvider(string cookieName, bool httpOnly = false)
        {
            _cookieName = cookieName;
            _httpOnly = httpOnly;
        }

        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var request = httpContext.Request;

            string queryCulture = null;

            var path = request.Path;

            if (path.HasValue)
            {
                var pathString = path.Value;
                
                if (!string.IsNullOrEmpty(pathString))
                {
                    var pathParts = pathString.Split("/", StringSplitOptions.RemoveEmptyEntries);

                    if (pathParts.Length > 0)
                    {
                        var firstPathPart = pathParts[0];
                        
                        if (firstPathPart.Length <= 3)
                        {
                            try
                            {
                                var cultureInfo = CultureInfo.GetCultureInfo(firstPathPart);

                                queryCulture = cultureInfo.TwoLetterISOLanguageName;
                            }
                            catch
                            {
                                // ignore ...
                            }
                        }
                    }

                }
            }

            if (request.QueryString.HasValue)
            {
                if (string.IsNullOrEmpty(queryCulture) && !string.IsNullOrWhiteSpace(QueryStringKey))
                {
                    queryCulture = request.Query[QueryStringKey];
                }
            }

            if (string.IsNullOrEmpty(queryCulture))
            {
                // No values specified for either so no match
                return NullProviderCultureResult;
            }

            var desiredCookieValue = $"c={queryCulture}|uic={queryCulture}";

            if (!httpContext.Request.Cookies.ContainsKey(_cookieName) ||
                !string.Equals(httpContext.Request.Cookies[_cookieName], desiredCookieValue))
            {
                // add or change the cookie to the new value

                httpContext.Response.Cookies.Append(_cookieName, desiredCookieValue, new CookieOptions()
                {
                    Expires = DateTime.Now.AddYears(1),
                    Secure = true,
                    // was LAX
                    SameSite = SameSiteMode.None,
                    HttpOnly = _httpOnly
                });
            }

            var providerResultCulture = new ProviderCultureResult(queryCulture, queryCulture);

            return Task.FromResult(providerResultCulture);
        }
    }
}
