using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;

namespace HCore.Identity.PagesUI.Classes.Pages
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