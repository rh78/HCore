﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;

namespace HCore.Tenants.Providers.Impl
{
    internal class NonHttpContextUrlProviderImpl : INonHttpContextUrlProvider
    {
        public string BaseUrl { get; private set; }

        public string WebUrl { get; private set; }

        public string EcbBackendApiUrl { get; private set; }
        public string PortalsBackendApiUrl { get; private set; }

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

            EcbBackendApiUrl = tenantInfoAccessor.TenantInfo.EcbBackendApiUrl;
            PortalsBackendApiUrl = tenantInfoAccessor.TenantInfo.PortalsBackendApiUrl;
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
