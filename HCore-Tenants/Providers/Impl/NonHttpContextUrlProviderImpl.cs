using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;

namespace HCore.Tenants.Providers.Impl
{
    internal class NonHttpContextUrlProviderImpl : INonHttpContextUrlProvider
    {
        public string BaseUrl { get; private set; }

        public string WebUrl { get; private set; }

        public string BackendApiUrl { get; private set; }
        public string FrontendApiUrl { get; private set; }

        public NonHttpContextUrlProviderImpl(IHttpContextAccessor httpContextAccessor, ITenantInfoAccessor tenantInfoAccessor, IConfiguration configuration)
        {
            string baseUrl = configuration["WebServer:BaseUrl"];

            if (!string.IsNullOrEmpty(baseUrl))
                BaseUrl = baseUrl;
            else
                BaseUrl = null;

            WebUrl = tenantInfoAccessor.TenantInfo.WebUrl;

            FrontendApiUrl = tenantInfoAccessor.TenantInfo.FrontendApiUrl;
            BackendApiUrl = tenantInfoAccessor.TenantInfo.BackendApiUrl;
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
