using HCore.Tenants.Models;
using Microsoft.AspNetCore.Http;

namespace HCore.Tenants.Providers.Impl
{
    internal class TenantInfoAccessorImpl : ITenantInfoAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantInfoAccessorImpl(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ITenantInfo TenantInfo => _httpContextAccessor.HttpContext?.GetTenantInfo();

        public string MatchedSubDomain => _httpContextAccessor.HttpContext?.GetMatchedSubDomain();
    }
}
