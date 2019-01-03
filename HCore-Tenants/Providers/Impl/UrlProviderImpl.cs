using Microsoft.AspNetCore.Http;
using System;

namespace HCore.Tenants.Providers.Impl
{
    internal class UrlProviderImpl : IUrlProvider
    {
        public string BaseUrl { get; private set; }

        public string WebUrl { get; private set; }

        public string BackendApiUrl { get; private set; }
        public string FrontendApiUrl { get; private set; }

        public UrlProviderImpl(IHttpContextAccessor httpContextAccessor, ITenantInfoAccessor tenantInfoAccessor)
        {
            var request = httpContextAccessor.HttpContext?.Request;

            if (request != null)
                BaseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/";
            else
                BaseUrl = null;

            WebUrl = tenantInfoAccessor.TenantInfo.WebUrl;

            BackendApiUrl = tenantInfoAccessor.TenantInfo.BackendApiUrl;
            FrontendApiUrl = tenantInfoAccessor.TenantInfo.FrontendApiUrl;
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

        public string BuildBackendApiUrl(string path)
        {
            if (string.IsNullOrEmpty(BackendApiUrl))
                throw new Exception("No backend API URL is set up for this service");

            return BackendApiUrl + path;
        }

        public string BuildFrontendApiUrl(string path)
        {
            if (string.IsNullOrEmpty(FrontendApiUrl))
                throw new Exception("No frontend API URL is set up for this service");

            return FrontendApiUrl + path;
        }
    }
}
