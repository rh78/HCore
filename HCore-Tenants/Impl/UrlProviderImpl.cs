using HCore.Web.Providers;
using System;

namespace HCore.Tenants.Impl
{
    internal class UrlProviderImpl : IUrlProvider
    {
        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        public string ApiDomain { get => _tenantInfoAccessor.TenantInfo?.ApiUrl; }

        public string WebDomain { get => _tenantInfoAccessor.TenantInfo?.WebUrl; }

        public UrlProviderImpl(ITenantInfoAccessor tenantInfoAccessor)
        {
            _tenantInfoAccessor = tenantInfoAccessor;
        }        

        public string BuildApiUrl(string path)
        {
            if (string.IsNullOrEmpty(ApiDomain))
                throw new Exception("No API domain is set up for this service");

            return ApiDomain + path;
        }

        public string BuildWebUrl(string path)
        {
            if (string.IsNullOrEmpty(WebDomain))
                throw new Exception("No web domain is set up for this service");

            return WebDomain + path;
        }
    }
}
