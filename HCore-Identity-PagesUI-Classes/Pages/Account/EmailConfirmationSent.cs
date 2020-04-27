using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [TypeFilter(typeof(SecurityHeadersAttribute))]
    public class EmailConfirmationSentModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
