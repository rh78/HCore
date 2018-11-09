using Microsoft.AspNetCore.Http;
using System;

namespace HCore.Tenants.Providers.Impl
{
    internal class UrlProviderImpl : IUrlProvider
    {
        public string BaseUrl { get; private set; }
        public string WebUrl { get; private set; }
        public string ApiUrl { get; private set; }

        public UrlProviderImpl(IHttpContextAccessor httpContextAccessor, ITenantInfoAccessor tenantInfoAccessor)
        {
            var request = httpContextAccessor.HttpContext?.Request;

            if (request != null)
                BaseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/";
            else
                BaseUrl = null;

            WebUrl = tenantInfoAccessor.TenantInfo.WebUrl;
            ApiUrl = tenantInfoAccessor.TenantInfo.ApiUrl;
        }

        public string BuildUrl(string path)
        {
            if (string.IsNullOrEmpty(BaseUrl))
                throw new Exception("No base url is available");

            return BaseUrl + path;
        }

        public string BuildWebUrl(string path)
        {
            if (string.IsNullOrEmpty(WebUrl))
                throw new Exception("No web URL is set up for this service");

            return WebUrl + path;
        }

        public string BuildApiUrl(string path)
        {
            if (string.IsNullOrEmpty(WebUrl))
                throw new Exception("No web URL is set up for this service");

            return WebUrl + path;
        }
    }
}
