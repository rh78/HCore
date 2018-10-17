using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.Generated.Controllers;
using HCore.Identity.Generated.Models;
using HCore.Web.Exceptions;

namespace HCore.Identity.PagesUI.Classes.Pages.Account.Manage
{
    [Authorize]
    [SecurityHeaders]
    public class ChangePasswordModel : PageModel
    {
        private readonly ISecureApiController _secureApiController;

        public ChangePasswordModel(
            ISecureApiController secureApiController)
        {
            _secureApiController = secureApiController;
        }

        [BindProperty]
        public SetUserPasswordSpec Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            var userUuid = User.FindFirstValue(IdentityModel.JwtClaimTypes.Subject);

            var apiResult = await _secureApiController.GetUserAsync(userUuid).ConfigureAwait(false);            
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                var userUuid = User.FindFirstValue(IdentityModel.JwtClaimTypes.Subject);

                await _secureApiController.SetUserPasswordAsync(userUuid, Input).ConfigureAwait(false);

                StatusMessage = "Your password has been changed";

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
