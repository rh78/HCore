using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HCore.Identity.Attributes;
using HCore.Tenants.Providers;
using HCore.Web.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class TenantModel : BasePageModelProvidingJsonModelData
    {
        public override string ModelAsJson { get; } = "{}";

        public static readonly Regex Tenant = new Regex(@"^[a-zA-Z0-9\-]+$");
        public static readonly string CookieName = "HCore.Tenant.Selection";

        [BindProperty]
        public string Domain { get; set; }

        private readonly ITenantDataProvider _tenantDataProvider;

        public TenantModel(ITenantDataProvider tenantDataProvider)
        {
            _tenantDataProvider = tenantDataProvider;
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
            else if (Request.Cookies.ContainsKey(CookieName))
            {
                tenantName = Request.Cookies[CookieName];
            }

            try
            {
                return await HandleDomainAsync(tenantName).ConfigureAwait(false);
            } 
            catch (Exception)
            {
                // ignore it
            }

            return LocalRedirect("~/");
        }

        private async Task<IActionResult> HandleDomainAsync(string domain)
        {
            domain = domain?.Trim();

            if (string.IsNullOrEmpty(domain))
                throw new RequestFailedApiException(RequestFailedApiException.DomainMissing, "The domain is missing");

            domain = domain.ToLower();

            if (!Tenant.IsMatch(domain))
                throw new RequestFailedApiException(RequestFailedApiException.DomainInvalid, "The domain is invalid");

            var (_, tenantInfo) = await _tenantDataProvider.GetTenantByHostAsync($"{domain}.smint.io").ConfigureAwait(false);

            if (tenantInfo == null)
                throw new RequestFailedApiException(RequestFailedApiException.DomainNotFound, "The domain was not found");

            return Redirect(tenantInfo.WebUrl);
        }
    }
}
