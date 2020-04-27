using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace HCore.Identity.PagesUI.Classes.Pages
{
    [Authorize]
    [ServiceFilter(typeof(SecurityHeadersAttribute))]
    public class LoginModel : PageModel
    {
        public IActionResult OnGet()
        {
            return LocalRedirect("~/");
        }
    }
}