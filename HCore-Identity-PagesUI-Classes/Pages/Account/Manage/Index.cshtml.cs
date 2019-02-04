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

namespace HCore.Identity.PagesUI.Classes.Pages.Account.Manage
{
    [Authorize]
    [SecurityHeaders]
    public partial class IndexModel : PageModel
    {
        private readonly IIdentityServices _identityServices;

        public IndexModel(
            IIdentityServices identityServices)
        {
            _identityServices = identityServices;
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
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber
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
                ModelState.AddModelError(string.Empty, e.Message);

                if (user != null)
                {
                    Input.Email = user.Email;

                    Email = user.Email;
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
                ModelState.AddModelError(string.Empty, e.Message);

                if (user != null)
                {
                    Input.Email = user.Email;

                    Email = user.Email;
                    EmailConfirmed = user.EmailConfirmed;
                }
            }

            return Page();
        }
    }
}
