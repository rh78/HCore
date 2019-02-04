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

namespace HCore.Identity.PagesUI.Classes.Pages.Account.Manage
{
    [Authorize]
    [SecurityHeaders]
    public class ChangePasswordModel : PageModel
    {
        private readonly IIdentityServices _identityServices;

        public ChangePasswordModel(
            IIdentityServices identityServices)
        {
            _identityServices = identityServices;
        }

        [BindProperty]
        public SetUserPasswordSpec Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            var userUuid = User.GetUserUuid();

            var apiResult = await _identityServices.GetUserAsync(userUuid).ConfigureAwait(false);            
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                var userUuid = User.GetUserUuid();

                await _identityServices.SetUserPasswordAsync(userUuid, Input).ConfigureAwait(false);

                StatusMessage = Messages.your_password_has_been_changed;

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
