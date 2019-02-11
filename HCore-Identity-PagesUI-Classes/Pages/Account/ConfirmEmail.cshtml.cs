using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Web.Exceptions;
using HCore.Identity.Models;
using HCore.Identity.Services;
using HCore.Translations.Providers;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class ConfirmEmailModel : PageModel
    {
        private readonly IIdentityServices _identityServices;
        private readonly ITranslationsProvider _translationsProvider;

        public ConfirmEmailModel(
            IIdentityServices identityServices)
        {
            _identityServices = identityServices;
        }

        public async Task<IActionResult> OnGetAsync(string userUuid, string code)
        {
            ModelState.Clear();

            try
            {
                await _identityServices.ConfirmUserEmailAddressAsync(userUuid, new UserConfirmEmailSpec()
                {                    
                    Code = code
                }).ConfigureAwait(false);

                return Page();
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));

                return LocalRedirect("~/");
            }            
        }
    }
}
