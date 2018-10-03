using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReinhardHolzner.Core.Identity.Generated.Controllers;
using ReinhardHolzner.Core.Identity.Generated.Models;
using ReinhardHolzner.Core.Web.Exceptions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using ReinhardHolzner.Core.Identity.Attributes;
using IdentityServer4.Models;
using ReinhardHolzner.Core.Web.Result;
using IdentityServer4.Events;
using Microsoft.AspNetCore.Authorization;

namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class LoginModel : PageModel
    {
        private readonly ISecureApiController _secureApiController;

        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;

        public LoginModel(
            ISecureApiController secureApiController,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events)
        {
            _secureApiController = secureApiController;
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
        }

        public string UserName { get; set; }
        public bool EnableLocalLogin { get; set; }        

        [BindProperty]
        public string ReturnUrl { get; set; }

        [BindProperty]
        public UserSignInSpec Input { get; set; }

        public async Task OnGetAsync(string returnUrl = null)
        {            
            await PrepareModelAsync(returnUrl).ConfigureAwait(false);            
        }
        
        public async Task<IActionResult> OnPostAsync(string action = null)
        {
            if (!string.Equals(action, "submit"))
            {
                // the user clicked the "cancel" button
                var context = await _interaction.GetAuthorizationContextAsync(ReturnUrl);

                if (context != null)
                {
                    // If the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.

                    await _interaction.GrantConsentAsync(context, ConsentResponse.Denied);

                    // We can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null

                    return Redirect(ReturnUrl);
                }
                else
                {
                    // Since we don't have a valid context, then we just go back to the home page

                    return LocalRedirect("~/");
                }
            }

            ModelState.Clear();

            try
            {
                ApiResult<User> result = await _secureApiController.SignInUserAsync(Input).ConfigureAwait(false);

                User user = result.Result;

                await _events.RaiseAsync(new UserLoginSuccessEvent(user.Email, user.Uuid, user.Email)).ConfigureAwait(false);

                if (_interaction.IsValidReturnUrl(ReturnUrl) || Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }

                return LocalRedirect("~/");
            }
            catch (ApiException e)
            {
                await _events.RaiseAsync(new UserLoginFailureEvent(Input.Email, "Invalid credentials"));

                ModelState.AddModelError(string.Empty, e.Message);
            }            

            await PrepareModelAsync(ReturnUrl);

            return Page();            
        }

        private async Task PrepareModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            EnableLocalLogin = false;

            if (context?.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);

                if (client != null)               
                    EnableLocalLogin = client.EnableLocalLogin;                                    
            }

            ReturnUrl = returnUrl;
            UserName = context?.LoginHint;
        }
    }
}
