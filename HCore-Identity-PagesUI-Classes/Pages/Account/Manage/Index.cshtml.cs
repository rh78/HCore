using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.Models;
using HCore.Web.Exceptions;
using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.Services;
using HCore.Identity.Resources;
using HCore.Translations.Providers;

namespace HCore.Identity.PagesUI.Classes.Pages.Account.Manage
{
    [Authorize]
    [SecurityHeaders]
    public partial class IndexModel : PageModel
    {
        private readonly IIdentityServices _identityServices;
        private readonly ITranslationsProvider _translationsProvider;

        public IndexModel(
            IIdentityServices identityServices,
            ITranslationsProvider translationsProvider)
        {
            _identityServices = identityServices;
            _translationsProvider = translationsProvider;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public UserSpec Input { get; set; }

        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }

        public async Task OnGetAsync()
        {
            var userUuid = User.GetUserUuid();

            var user = await _identityServices.GetUserAsync(userUuid).ConfigureAwait(false);

            Input = new UserSpec()
            {
                Email = user.GetEmail(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                NotificationCulture = user.NotificationCulture,
                Currency = user.Currency
            };

            Email = Input.Email;
            EmailConfirmed = user.EmailConfirmed;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            UserModel user = null;

            try
            {
                var userUuid = User.GetUserUuid();

                user = await _identityServices.GetUserAsync(userUuid).ConfigureAwait(false);

                await _identityServices.UpdateUserAsync(userUuid, Input, false).ConfigureAwait(false);

                StatusMessage = Messages.your_profile_has_been_updated;

                return RedirectToPage();
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));

                if (user != null)
                {
                    Input.Email = user.GetEmail();

                    Email = user.GetEmail();
                    EmailConfirmed = user.EmailConfirmed;
                }
            }

            return Page();                                  
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            ModelState.Clear();

            UserModel user = null;

            try
            {
                var userUuid = User.GetUserUuid();

                user = await _identityServices.GetUserAsync(userUuid).ConfigureAwait(false);

                await _identityServices.ResendUserEmailConfirmationEmailAsync(userUuid).ConfigureAwait(false);

                StatusMessage = Messages.verification_email_resent;

                return RedirectToPage();
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));

                if (user != null)
                {
                    Input.Email = user.GetEmail();

                    Email = user.GetEmail();
                    EmailConfirmed = user.EmailConfirmed;
                }
            }

            return Page();
        }
    }
}
