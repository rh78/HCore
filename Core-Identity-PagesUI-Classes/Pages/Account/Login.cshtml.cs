using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReinhardHolzner.Core.Identity.AuthAPI.Generated.Controllers;
using ReinhardHolzner.Core.Identity.AuthAPI.Generated.Models;
using ReinhardHolzner.Core.Web.Exceptions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using ReinhardHolzner.Core.Identity.Attributes;

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
        public string ReturnUrl { get; set; }

        [BindProperty]
        public UserSignInSpec Input { get; set; }

        public async Task OnGetAsync(string returnUrl = null)
        {            
            await PrepareModelAsync(returnUrl);            
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                await _secureApiController.SignInUserAsync(Input).ConfigureAwait(false);

                return LocalRedirect("~/");
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
            }

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
