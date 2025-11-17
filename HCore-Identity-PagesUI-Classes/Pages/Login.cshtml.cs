using HCore.Identity.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HCore.Identity.PagesUI.Classes.Pages
{
    [Authorize]
    [SecurityHeaders]
    public class LoginModel : PageModel
    {
        private string _scriptNonce = null;

        public IActionResult OnGet()
        {
            return LocalRedirect("~/");
        }

        public string GetScriptNonce()
        {
            if (_scriptNonce == null)
            {
                _scriptNonce = HttpContext.GetScriptNonce();
            }

            return _scriptNonce;
        }
    }
}