using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Web.Exceptions;
using HCore.Identity.ViewModels;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class EmailNotConfirmedModel : PageModel
    {
        private readonly IIdentityServices _identityServices;

        public EmailNotConfirmedModel(
            IIdentityServices identityServices)
        {
            _identityServices = identityServices;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public UserUuidSpec Input { get; set; }

        public IActionResult OnGet(string userUuid = null)
        {
            if (userUuid == null)
            {
                return BadRequest("A user UUID must be specified.");
            }
            else
            {
                Input = new UserUuidSpec
                {
                    UserUuid = userUuid
                };

                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
                {
                await _identityServices.ResendUserEmailConfirmationEmailAsync(Input.UserUuid).ConfigureAwait(false);

                return RedirectToPage("./EmailConfirmationSent");
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
            }

            return Page();
        }
    }
}
