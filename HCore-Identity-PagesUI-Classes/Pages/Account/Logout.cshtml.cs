using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class LogoutModel : PageModel
    {
        private readonly IIdentityServices _identityServices;

        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;

        [BindProperty]
        public bool ShowLogoutPrompt { get; set; }

        [BindProperty]
        public bool LoggedOut { get; set; }

        [BindProperty]
        public bool AutomaticRedirectAfterSignOut { get; set; }

        [BindProperty]
        public string ClientName { get; set; }
        public bool IsLocalLogout { get; set; }

        [BindProperty]
        public string PostLogoutRedirectUri { get; set; }

        [BindProperty]
        public string SignOutIframeUrl { get; set; }

        [BindProperty]
        public string LogoutId { get; set; }

        public LogoutModel(
             IIdentityServices identityServices,
             IIdentityServerInteractionService interaction,
             IEventService events)
        {
            _identityServices = identityServices;
            _interaction = interaction;
            _events = events;
        }

        public async Task<IActionResult> OnGet(string logoutId = null)
        {
            // Build a model so the logout page knows what to display

            await PrepareLogoutModelAsync(logoutId).ConfigureAwait(false);

            if (!ShowLogoutPrompt)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.

                LogoutId = logoutId;

                return await OnPost().ConfigureAwait(false);
            }

            return Page();
        }

        private async Task PrepareLogoutModelAsync(string logoutId)
        {
            LogoutId = logoutId;
            ShowLogoutPrompt = true;
           
            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page

                ShowLogoutPrompt = false;                

                return;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);

            if (context == null || string.IsNullOrEmpty(context.SessionId))
            {
                IsLocalLogout = true;

                PostLogoutRedirectUri = "/";

                return;
            }

            if (context.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out

                ShowLogoutPrompt = false;

                return;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.            
        }

        public async Task<IActionResult> OnPost()
        {
            // build a model so the logged out page knows what to display

            await PrepareLoggedOutModelAsync(LogoutId).ConfigureAwait(false);

            if (User?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie

                await _identityServices.SignOutUserAsync().ConfigureAwait(false);

                // raise the logout event
                
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            LoggedOut = true;

            return Page();
        }

        private async Task PrepareLoggedOutModelAsync(string logoutId)
        {
            AutomaticRedirectAfterSignOut = true;

            // get context information (client name, post logout redirect URI and iframe for federated signout)

            var context = await _interaction.GetLogoutContextAsync(logoutId);

            if (context == null || string.IsNullOrEmpty(context.SessionId))
            {
                IsLocalLogout = true;

                PostLogoutRedirectUri = "/";

                return;
            }
            
            PostLogoutRedirectUri = context.PostLogoutRedirectUri;
            ClientName = string.IsNullOrEmpty(context.ClientName) ? context.ClientId : context.ClientName;
            SignOutIframeUrl = context.SignOutIFrameUrl;
            LogoutId = logoutId;        
        }
    }
}