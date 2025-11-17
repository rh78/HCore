using HCore.Identity.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HCore.Identity.PagesUI.Classes.Pages
{
    [SecurityHeaders]
    public class LogoutModel : PageModel
    {
        private string _scriptNonce = null;

        public string GetScriptNonce()
        {
            if (_scriptNonce == null)
            {
                _scriptNonce = HttpContext.GetScriptNonce();
            }

            return _scriptNonce;
        }

        public IActionResult OnPost()
        {
            return LocalRedirect("/Account/Logout");
        }
    }
}