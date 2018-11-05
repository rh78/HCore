using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.ViewModels;
using HCore.Web.Exceptions;

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
                PhoneNumber = user.PhoneNumber
            };

            Email = Input.Email;
            EmailConfirmed = user.EmailConfirmed;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                var userUuid = User.GetUserUuid();

                await _identityServices.UpdateUserAsync(userUuid, Input).ConfigureAwait(false);

                StatusMessage = "Your profile has been updated";

                return RedirectToPage();
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
            }

            return Page();                                  
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var userUuid = User.GetUserUuid();

            try
            {
                await _identityServices.ResendUserEmailConfirmationEmailAsync(userUuid).ConfigureAwait(false);

                StatusMessage = "Verification email sent, please check your email";

                return RedirectToPage();
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
            }

            return Page();
        }
    }
}
