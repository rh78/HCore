using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HCore.Identity.Attributes;
using HCore.Tenants.Providers;
using HCore.Web.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class TenantPopupModel : BasePageModelProvidingJsonModelData
    {
        public static readonly Regex Tenant = new Regex(@"^[a-zA-Z0-9\-]+$");

        [BindProperty]
        public string Domain { get; set; }

        private readonly ITenantDataProvider _tenantDataProvider;

        private static string _hostPattern;

        public override string ModelAsJson { get; } = "{}";

        public TenantPopupModel(
            ITenantDataProvider tenantDataProvider,
            IConfiguration configuration)
        {
            _tenantDataProvider = tenantDataProvider;

            if (_hostPattern == null)
            {
                _hostPattern = configuration["WebServer:HostPattern"];

                if (string.IsNullOrEmpty(_hostPattern))
                    _hostPattern = ".smint.io";
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            string tenantName = null;

            var hostName = HttpContext.Request.Host.Host?.Split('.')[0].ToLower();

            if (!string.Equals(hostName, "login") &&
                !string.Equals(hostName, "development-login"))
            {
                tenantName = hostName;
            }

            try
            {
                await HandleDomainAsync(tenantName).ConfigureAwait(false);
            } 
            catch (Exception)
            {
                // ignore it
            }

            return Page();
        }

        private async Task HandleDomainAsync(string domain)
        {
            domain = domain?.Trim();

            if (string.IsNullOrEmpty(domain))
                throw new RequestFailedApiException(RequestFailedApiException.DomainMissing, "The domain is missing");

            domain = domain.ToLower();

            if (!Tenant.IsMatch(domain))
                throw new RequestFailedApiException(RequestFailedApiException.DomainInvalid, "The domain is invalid");

            var (_, tenantInfo) = await _tenantDataProvider.GetTenantByHostAsync($"{domain}{_hostPattern}").ConfigureAwait(false);

            if (tenantInfo == null)
                throw new RequestFailedApiException(RequestFailedApiException.DomainNotFound, "The domain was not found");
        }
    }
}
