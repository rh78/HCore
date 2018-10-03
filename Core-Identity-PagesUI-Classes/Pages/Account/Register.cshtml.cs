using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReinhardHolzner.Core.Identity.Attributes;
using ReinhardHolzner.Core.Identity.Generated.Controllers;
using ReinhardHolzner.Core.Identity.Generated.Models;
using ReinhardHolzner.Core.Web.Exceptions;
using ReinhardHolzner.Core.Web.Result;

namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class RegisterModel : PageModel
    {
        private readonly ISecureApiController _secureApiController;
        private readonly IEventService _events;

        public RegisterModel(
            ISecureApiController secureApiController,
            IEventService events)
        {
            _secureApiController = secureApiController;
            _events = events;
        }

        [BindProperty]
        public UserSpec Input { get; set; }

        public void OnGet()
        {
            
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                ApiResult<User> result = await _secureApiController.CreateUserAsync(Input).ConfigureAwait(false);

                User user = result.Result;

                await _events.RaiseAsync(new UserLoginSuccessEvent(user.Email, user.Uuid, user.Email)).ConfigureAwait(false);

                return LocalRedirect("~/");
            } catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
            }            

            return Page();
        }
    }
}