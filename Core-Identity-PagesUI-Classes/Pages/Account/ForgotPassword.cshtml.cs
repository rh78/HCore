using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReinhardHolzner.Core.Identity.Attributes;
using ReinhardHolzner.Core.Identity.Generated.Controllers;
using ReinhardHolzner.Core.Identity.Generated.Models;
using ReinhardHolzner.Core.Web.Exceptions;

namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Pages.Account
{
    [AllowAnonymous]
    [SecurityHeaders]
    public class ForgotPasswordModel : PageModel
    {
        private readonly ISecureApiController _secureApiController;

        public ForgotPasswordModel(
            ISecureApiController secureApiController)
        {
            _secureApiController = secureApiController;
        }

        [BindProperty]
        public UserForgotPasswordSpec Input { get; set; }
        
        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                await _secureApiController.UserForgotPasswordAsync(Input).ConfigureAwait(false);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
            }

            return Page();            
        }
    }
}
