using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HCore.Identity.Attributes;
using HCore.Web.Exceptions;
using HCore.Identity.Models;
using HCore.Identity.Services;
using HCore.Translations.Providers;
using HCore.Identity.Database.SqlServer.Models.Impl;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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

        public async Task<IActionResult> OnGetAsync(string userUuid, string code, string success)
        {
            ModelState.Clear();

            try
            {
                if (string.IsNullOrEmpty(success))
                {
                    UserModel user = await _identityServices.ConfirmUserEmailAddressAsync(userUuid, new UserConfirmEmailSpec()
                    {
                        Code = code
                    }).ConfigureAwait(false);

                    // make sure we continue on THIS tenant when completing the login
                    // this avoids breaking wizards etc. by redirecting to another tenant 
                    // that was selected before

                    Response.Cookies.Delete(TenantModel.CookieName);

                    return LocalRedirect("/Account/ConfirmEmail?success=true");
                }
                else
                {
                    return Page();
                }
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));

                return LocalRedirect("~/");
            }            
        }
    }
}
