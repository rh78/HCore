using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReinhardHolzner.Core.Identity.AuthAPI.Generated.Controllers;

namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly ISecureApiController _secureApiController;
        
        public LogoutModel(
             ISecureApiController secureApiController)
        {
            _secureApiController = secureApiController;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {
            await _secureApiController.SignOutUserAsync().ConfigureAwait(false);

            return LocalRedirect("~/");            
        }
    }
}