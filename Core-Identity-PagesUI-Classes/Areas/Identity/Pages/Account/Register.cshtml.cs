using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReinhardHolzner.Core.Identity.AuthAPI.Generated.Controllers;
using ReinhardHolzner.Core.Identity.AuthAPI.Generated.Models;
using ReinhardHolzner.Core.Web.Exceptions;

namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly ISecureApiController _secureApiController;

        public RegisterModel(
            ISecureApiController secureApiController)
        {
            _secureApiController = secureApiController;
        }

        [BindProperty]
        public UserSpec Input { get; set; }

        public void OnGet()
        {
            
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                await _secureApiController.RegisterUserAsync(Input).ConfigureAwait(false);

                return LocalRedirect("~/");
            } catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
            }            

            return Page();
        }
    }
}