using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReinhardHolzner.Core.Identity.AuthAPI.Generated.Controllers;
using ReinhardHolzner.Core.Identity.AuthAPI.Generated.Models;
using ReinhardHolzner.Core.Web.Exceptions;

namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly ISecureApiController _secureApiController;

        public IndexModel(
            ISecureApiController secureApiController)
        {
            _secureApiController = secureApiController;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public User Input { get; set; }

        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userUuid = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var apiResult = await _secureApiController.GetUserAsync(userUuid).ConfigureAwait(false);

            Input = apiResult.Result;

            Email = Input.Email;
            EmailConfirmed = Input.EmailConfirmed != null && (bool)Input.EmailConfirmed;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                var userUuid = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _secureApiController.UpdateUserAsync(userUuid, Input).ConfigureAwait(false);

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
            var userUuid = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _secureApiController.ResendUserEmailConfirmationEmailAsync(userUuid).ConfigureAwait(false);

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
