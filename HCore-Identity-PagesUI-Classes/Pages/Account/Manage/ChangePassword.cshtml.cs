using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.Models;
using HCore.Web.Exceptions;
using HCore.Identity.Services;
using HCore.Identity.Resources;
using HCore.Translations.Providers;
using Newtonsoft.Json;
using HCore.Identity.Database.SqlServer.Models.Impl;

namespace HCore.Identity.PagesUI.Classes.Pages.Account.Manage
{
    [Authorize]
    [SecurityHeaders]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ChangePasswordModel : BasePageModelProvidingJsonModelData
    {
        private readonly IIdentityServices _identityServices;
        private readonly ITranslationsProvider _translationsProvider;

        public ChangePasswordModel(
            IIdentityServices identityServices,
            ITranslationsProvider translationsProvider)
        {
            _identityServices = identityServices;
            _translationsProvider = translationsProvider;
        }

        public override string ModelAsJson { get =>
            JsonConvert.SerializeObject(
                new
                {
                    PasswordChangePossible,
                    StatusMessage,
                }, new JsonSerializerSettings()
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                }
            );
        }

        [BindProperty]
        public SetUserPasswordSpec Input { get; set; }

        public bool PasswordChangePossible { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            var userUuid = User.GetUserUuid();

            var userModel = await _identityServices.GetUserAsync(userUuid).ConfigureAwait(false);

            PasswordChangePossible = userModel.PasswordHash != null;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            UserModel userModel = null;

            try
            {
                var userUuid = User.GetUserUuid();

                userModel = await _identityServices.GetUserAsync(userUuid).ConfigureAwait(false);

                PasswordChangePossible = userModel.PasswordHash != null;

                await _identityServices.SetUserPasswordAsync(userUuid, Input).ConfigureAwait(false);

                StatusMessage = Messages.your_password_has_been_changed;

                return RedirectToPage();
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));

                if (userModel != null)
                {
                    PasswordChangePossible = userModel.PasswordHash != null;
                }
            }

            return Page();
        }
    }
}
