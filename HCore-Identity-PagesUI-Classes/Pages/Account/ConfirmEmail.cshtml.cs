using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Web.Exceptions;
using HCore.Identity.Models;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class ConfirmEmailModel : PageModel
    {
        private readonly IIdentityServices _identityServices;

        public ConfirmEmailModel(
            IIdentityServices identityServices)
        {
            _identityServices = identityServices;
        }

        public async Task<IActionResult> OnGetAsync(string userUuid, string code)
        {
            ModelState.Clear();

            try
            {
                await _identityServices.ConfirmUserEmailAddressAsync(userUuid, new UserConfirmEmailSpec()
                {                    
                    Code = code
                }).ConfigureAwait(false);

                return Page();
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);

                return LocalRedirect("~/");
            }            
        }
    }
}
