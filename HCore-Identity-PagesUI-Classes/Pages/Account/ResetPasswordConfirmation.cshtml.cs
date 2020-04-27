using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [ServiceFilter(typeof(SecurityHeadersAttribute))]
    public class ResetPasswordConfirmationModel : PageModel
    {
        public void OnGet()
        {

        }
    }
}
