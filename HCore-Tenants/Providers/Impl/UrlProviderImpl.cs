using Microsoft.AspNetCore.Http;
using System;

namespace HCore.Tenants.Providers.Impl
{
    internal class UrlProviderImpl : IUrlProvider
    {
        public string BaseUrl { get; private set; }

        public string WebUrl { get; private set; }

        public string EcbBackendApiUrl { get; private set; }
        public string PortalsBackendApiUrl { get; private set; }

        public string FrontendApiUrl { get; private set; }

        public UrlProviderImpl(IHttpContextAccessor httpContextAccessor, ITenantInfoAccessor tenantInfoAccessor)
        {
            var request = httpContextAccessor.HttpContext?.Request;

            if (request != null)
                BaseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/";
            else
                BaseUrl = null;

            if (tenantInfoAccessor.TenantInfo != null)
            {
                WebUrl = tenantInfoAccessor.TenantInfo.WebUrl;

                EcbBackendApiUrl = tenantInfoAccessor.TenantInfo.EcbBackendApiUrl;
                PortalsBackendApiUrl = tenantInfoAccessor.TenantInfo.PortalsBackendApiUrl;

                FrontendApiUrl = tenantInfoAccessor.TenantInfo.FrontendApiUrl;
            }
            else
            {
                WebUrl = null;

                EcbBackendApiUrl = null;
                PortalsBackendApiUrl = null;

                FrontendApiUrl = null;
            }
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

        public string BuildEcbBackendApiUrl(string path)
        {
            if (string.IsNullOrEmpty(EcbBackendApiUrl))
                throw new Exception("No ECB backend API URL is set up for this service");

            return EcbBackendApiUrl + path;
        }

        public string BuildPortalsBackendApiUrl(string path)
        {
            if (string.IsNullOrEmpty(PortalsBackendApiUrl))
                throw new Exception("No Portals backend API URL is set up for this service");

            return PortalsBackendApiUrl + path;
        }

        public string BuildFrontendApiUrl(string path)
        {
            if (string.IsNullOrEmpty(FrontendApiUrl))
                throw new Exception("No frontend API URL is set up for this service");

            return FrontendApiUrl + path;
        }
    }
}
