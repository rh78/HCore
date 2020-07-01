using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HCore.Identity.Attributes;
using HCore.Web.Exceptions;
using HCore.Identity.Models;
using HCore.Identity.Services;
using HCore.Translations.Providers;
using IdentityServer4.Services;
using IdentityServer4.Events;
using HCore.Identity.Database.SqlServer.Models.Impl;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class ConfirmEmailModel : BasePageModelProvidingJsonModelData
    {
        private readonly IIdentityServices _identityServices;
        private readonly ITranslationsProvider _translationsProvider;
        private readonly IEventService _events;

        public ConfirmEmailModel(
            IIdentityServices identityServices,
            IEventService events,
            ITranslationsProvider translationsProvider)
        {
            _identityServices = identityServices;
            _events = events;
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

                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.GetEmail())).ConfigureAwait(false);

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
