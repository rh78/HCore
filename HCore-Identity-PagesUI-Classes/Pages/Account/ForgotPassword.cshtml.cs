using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.Models;
using HCore.Web.Exceptions;
using HCore.Identity.Services;
using HCore.Translations.Providers;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class ForgotPasswordModel : PageModel
    {
        private readonly IIdentityServices _identityServices;
        private readonly ITranslationsProvider _translationsProvider;

        public ForgotPasswordModel(
            IIdentityServices identityServices,
            ITranslationsProvider translationsProvider)
        {
            _identityServices = identityServices;
            _translationsProvider = translationsProvider;
        }

        [BindProperty]
        public UserForgotPasswordSpec Input { get; set; }
        
        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                await _identityServices.UserForgotPasswordAsync(Input).ConfigureAwait(false);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));
            }

            return Page();            
        }
    }
}
