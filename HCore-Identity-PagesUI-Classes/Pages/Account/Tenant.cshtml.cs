using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HCore.Identity.Attributes;
using HCore.Tenants.Providers;
using HCore.Translations.Providers;
using HCore.Web.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class TenantModel : PageModel
    {
        private static readonly Regex Tenant = new Regex(@"^[a-zA-Z0-9\-]+$");
        private static readonly string CookieName = "HCore.Tenant.Selection";

        [BindProperty]
        public string Domain { get; set; }

        private readonly ITranslationsProvider _translationsProvider;
        private readonly ITenantDataProvider _tenantDataProvider;

        public TenantModel(ITranslationsProvider translationsProvider, ITenantDataProvider tenantDataProvider)
        {
            _translationsProvider = translationsProvider;
            _tenantDataProvider = tenantDataProvider;
        }

        public IActionResult OnGet()
        {
            if (Request.Cookies.ContainsKey(CookieName))
            {
                try
                {
                    string domain = Request.Cookies[CookieName];

                    return HandleDomain(domain);
                } 
                catch (Exception)
                {
                    // ignore it
                }
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            ModelState.Clear();

            try
            {
                return HandleDomain(Domain);
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));
            }

            return Page();
        }

        private IActionResult HandleDomain(string domain)
        {

            if (string.IsNullOrEmpty(domain))
                throw new RequestFailedApiException(RequestFailedApiException.DomainMissing, "The domain is missing");

            if (!Tenant.IsMatch(domain))
                throw new RequestFailedApiException(RequestFailedApiException.DomainInvalid, "The domain is invalid");

            var tenantInfo = _tenantDataProvider.LookupTenantByHost($"{domain}.smint.io");

            if (tenantInfo == null)
                throw new RequestFailedApiException(RequestFailedApiException.DomainNotFound, "The domain was not found");

            Response.Cookies.Append(CookieName, domain, new CookieOptions()
            {
                Domain = ".smint.io",
                Expires = DateTime.MaxValue
            });

            return Redirect(tenantInfo.WebUrl);
        }
    }
}
