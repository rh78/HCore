using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HCore.Identity.Attributes;
using HCore.Identity.Models;
using HCore.Web.Exceptions;
using HCore.Identity.Services;
using HCore.Translations.Providers;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class ResetPasswordModel : BasePageModelProvidingJsonModelData
    {
        private readonly IIdentityServices _identityServices;
        private readonly ITranslationsProvider _translationsProvider;

        public ResetPasswordModel(
            IIdentityServices identityServices,
            ITranslationsProvider translationsProvider)
        {
            _identityServices = identityServices;
            _translationsProvider = translationsProvider;
        }

        public override string ModelAsJson { get; } = "{}";

        [BindProperty]
        public ResetUserPasswordSpec Input { get; set; }

        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("A code must be supplied for password reset.");
            }
            else
            {
                Input = new ResetUserPasswordSpec
                {
                    Code = code
                };

                return Page();
            }
        }
       
        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                await _identityServices.ResetUserPasswordAsync(Input).ConfigureAwait(false);

                return RedirectToPage("./ResetPasswordConfirmation");
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));
            }

            return Page();
        }
    }
}
