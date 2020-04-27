using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;

namespace HCore.Identity.PagesUI.Classes.Pages
{
    [ServiceFilter(typeof(SecurityHeadersAttribute))]
    public class LogoutModel : PageModel
    {        
        public IActionResult OnPost()
        {
            return LocalRedirect("/Account/Logout");
        }        
    }
}