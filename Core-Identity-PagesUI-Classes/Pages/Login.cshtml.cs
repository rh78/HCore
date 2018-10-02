using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReinhardHolzner.Core.Identity.Attributes;

namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Pages
{
    [Authorize]
    [SecurityHeaders]
    public class LoginModel : PageModel
    {
        public void OnGet()
        {

        }
    }
}