using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.Models;
using HCore.Web.Exceptions;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class ForgotPasswordModel : PageModel
    {
        private readonly IIdentityServices _identityServices;

        public ForgotPasswordModel(
            IIdentityServices identityServices)
        {
            _identityServices = identityServices;
        }

        [BindProperty]
        public UserForgotPasswordSpec Input { get; set; }
        
        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                await _identityServices.UserForgotPasswordAsync(Input).ConfigureAwait(false);

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
