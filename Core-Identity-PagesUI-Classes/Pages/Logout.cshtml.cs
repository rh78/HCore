using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReinhardHolzner.Core.Identity.Attributes;

namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Pages
{
    [SecurityHeaders]
    public class LogoutModel : PageModel
    {        
        public IActionResult OnPost()
        {
            return LocalRedirect("/Account/Logout");
        }        
    }
}