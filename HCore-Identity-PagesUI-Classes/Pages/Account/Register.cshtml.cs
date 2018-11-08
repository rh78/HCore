using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.ViewModels;
using HCore.Web.Exceptions;
using HCore.Identity.Database.SqlServer.Models.Impl;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class RegisterModel : PageModel
    {
        private readonly IIdentityServices _identityServices;
        private readonly IIdentityServicesConfiguration _identityServicesConfiguration;
        private readonly IEventService _events;

        public RegisterModel(
            IIdentityServices identityServices,
            IIdentityServicesConfiguration identityServicesConfiguration,
            IEventService events)
        {
            _identityServices = identityServices;
            _identityServicesConfiguration = identityServicesConfiguration;
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
                UserModel user = await _identityServices.CreateUserAsync(Input, false).ConfigureAwait(false);

                if (_identityServicesConfiguration.RequireEmailConfirmed && !user.EmailConfirmed)
                {
                    return RedirectToPage("./EmailNotConfirmed", new { UserUuid = user.Id });
                } else
                {
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.Email, user.Id, user.Email)).ConfigureAwait(false);
                }                

                return LocalRedirect("~/");
            } catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
            }            

            return Page();
        }
    }
}