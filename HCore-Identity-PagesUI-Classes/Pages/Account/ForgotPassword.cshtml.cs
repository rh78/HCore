using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.Generated.Controllers;
using HCore.Identity.Generated.Models;
using HCore.Web.Exceptions;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
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
