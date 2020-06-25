using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HCore.Identity.Attributes;
using HCore.Web.Exceptions;
using HCore.Identity.Models;
using HCore.Identity.Services;
using HCore.Translations.Providers;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [TypeFilter(typeof(SecurityHeadersAttribute))]
    public class ConfirmEmailModel : BasePageModelProvidingJsonModelData
    {
        private readonly IIdentityServices _identityServices;
        private readonly ITranslationsProvider _translationsProvider;

        public ConfirmEmailModel(
            IIdentityServices identityServices,
            ITranslationsProvider translationsProvider)
        {
            _identityServices = identityServices;
            _translationsProvider = translationsProvider;
        }

        public override string ModelAsJson { get; } = "{}";

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
