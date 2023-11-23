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
using HCore.Tenants.Providers;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace HCore.Identity.PagesUI.Classes.Pages.Account.Manage
{
    [Authorize]
    [SecurityHeaders]
    public class ChangePasswordModel : BasePageModelProvidingJsonModelData
    {
        private readonly IIdentityServices _identityServices;
        private readonly ITranslationsProvider _translationsProvider;

        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        public ChangePasswordModel(
            IIdentityServices identityServices,
            ITranslationsProvider translationsProvider,
            IServiceProvider serviceProvider)
        {
            _identityServices = identityServices;
            _translationsProvider = translationsProvider;

            _tenantInfoAccessor = serviceProvider.GetService<ITenantInfoAccessor>();
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

            UpdatePasswordChangePossible(userModel);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            UserModel userModel = null;

            try
            {
                var userUuid = User.GetUserUuid();

                userModel = await _identityServices.GetUserAsync(userUuid).ConfigureAwait(false);

                UpdatePasswordChangePossible(userModel);

                await _identityServices.SetUserPasswordAsync(userUuid, Input).ConfigureAwait(false);

                StatusMessage = Messages.your_password_has_been_changed;

                return RedirectToPage();
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));

                UpdatePasswordChangePossible(userModel);
            }

            return Page();
        }

        private void UpdatePasswordChangePossible(UserModel userModel)
        {
            if (userModel == null)
            {
                PasswordChangePossible = false;

                return;
            }

            PasswordChangePossible = userModel.PasswordHash != null;

            if (_tenantInfoAccessor == null)
            {
                return;
            }

            var tenantInfo = _tenantInfoAccessor.TenantInfo;

            if (tenantInfo == null)
            {
                return;
            }
                    
            PasswordChangePossible = PasswordChangePossible || tenantInfo.ExternalAuthenticationAllowLocalLogin;
        }
    }
}
