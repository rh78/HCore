using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.Models;
using HCore.Web.Exceptions;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class ResetPasswordModel : PageModel
    {
        private readonly IIdentityServices _identityServices;

        public ResetPasswordModel(
            IIdentityServices identityServices)
        {
            _identityServices = identityServices;
        }

        [BindProperty]
        public ResetUserPasswordSpec Input { get; set; }

        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("A code must be supplied for password reset.");
            }
            else
            {
                Input = new ResetUserPasswordSpec
                {
                    Code = code
                };

                return Page();
            }
        }
       
        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                await _identityServices.ResetUserPasswordAsync(Input).ConfigureAwait(false);

                return RedirectToPage("./ResetPasswordConfirmation");
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
            }

            return Page();
        }
    }
}
