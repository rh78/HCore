using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HCore.Identity.Attributes;
using HCore.Tenants.Providers;
using HCore.Translations.Providers;
using HCore.Web.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class TenantModel : PageModel
    {
        public static readonly Regex Tenant = new Regex(@"^[a-zA-Z0-9\-]+$");

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
            return Page();
        }

        public IActionResult OnPost()
        {
            ModelState.Clear();

            try
            {
                if (string.IsNullOrEmpty(Domain))
                    throw new RequestFailedApiException(RequestFailedApiException.DomainMissing, "The domain is missing");

                if (!Tenant.IsMatch(Domain))
                    throw new RequestFailedApiException(RequestFailedApiException.DomainInvalid, "The domain is invalid");

                var tenantInfo = _tenantDataProvider.LookupTenantByHost($"{Domain}.smint.io");

                if (tenantInfo == null)
                    throw new RequestFailedApiException(RequestFailedApiException.DomainNotFound, "The domain was not found");

                return Redirect(tenantInfo.WebUrl);
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));
            }

            return Page();
        }
    }
}
