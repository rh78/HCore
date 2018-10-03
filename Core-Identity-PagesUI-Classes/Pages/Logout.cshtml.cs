using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
            return new SignOutResult(new string[] { IdentityCoreConstants.OidcScheme, IdentityCoreConstants.CookieScheme });
        }        
    }
}