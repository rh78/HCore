using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HCore.Identity.Attributes;
using HCore.Web.Exceptions;
using HCore.Identity.Models;
using HCore.Identity.Services;
using HCore.Translations.Providers;
using Microsoft.AspNetCore.DataProtection;
using System;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [TypeFilter(typeof(SecurityHeadersAttribute))]
    public class EmailNotConfirmedModel : BasePageModelProvidingJsonModelData
    {
        private readonly IIdentityServices _identityServices;
        private readonly ITranslationsProvider _translationsProvider;

        private readonly IDataProtectionProvider _dataProtectionProvider;

        public EmailNotConfirmedModel(
            IIdentityServices identityServices,
            ITranslationsProvider translationsProvider,
            IDataProtectionProvider dataProtectionProvider)
        {
            _identityServices = identityServices;
            _translationsProvider = translationsProvider;

            _dataProtectionProvider = dataProtectionProvider;
        }

        public override string ModelAsJson { get; } = "{}";

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public UserUuidSpec Input { get; set; }

        public IActionResult OnGet(string userUuid = null)
        {
            if (userUuid == null)
            {
                return BadRequest("A user UUID must be specified.");
            }
            else
            {
                try
                {
                    userUuid = _dataProtectionProvider.CreateProtector(nameof(EmailNotConfirmedModel)).Unprotect(userUuid);
                }
                catch (Exception)
                {
                    return BadRequest("The user UUID is invalid.");
                }

                Input = new UserUuidSpec
                {
                    UserUuid = userUuid
                };

                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                string userUuid;

                if (Input.UserUuid == null)
                    return BadRequest("A user UUID must be specified.");

                try
                {
                    userUuid = _dataProtectionProvider.CreateProtector(nameof(EmailNotConfirmedModel)).Unprotect(Input.UserUuid);
                }
                catch (Exception)
                {
                    return BadRequest("The user UUID is invalid.");
                }

                await _identityServices.ResendUserEmailConfirmationEmailAsync(userUuid).ConfigureAwait(false);

                return RedirectToPage("./EmailConfirmationSent");
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));
            }

            return Page();
        }
    }
}
