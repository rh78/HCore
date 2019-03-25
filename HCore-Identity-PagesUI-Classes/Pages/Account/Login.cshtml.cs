using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Models;
using HCore.Web.Exceptions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using HCore.Identity.Attributes;
using IdentityServer4.Models;
using IdentityServer4.Events;
using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.Services;
using HCore.Identity.Providers;
using HCore.Tenants.Providers;
using HCore.Segment.Providers;
using System;
using Segment.Model;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using HCore.Translations.Providers;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class LoginModel : PageModel
    {
        private readonly IIdentityServices _identityServices;
        private readonly IConfigurationProvider _configurationProvider;

        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;

        private readonly ISegmentProvider _segmentProvider;

        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        private readonly ITranslationsProvider _translationsProvider;

        public LoginModel(
            IIdentityServices identityServices,
            IConfigurationProvider configurationProvider,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            ITranslationsProvider translationsProvider,
            IServiceProvider serviceProvider)
        {
            _identityServices = identityServices;
            _configurationProvider = configurationProvider;
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;

            _segmentProvider = serviceProvider.GetService<ISegmentProvider>();

            _tenantInfoAccessor = serviceProvider.GetService<ITenantInfoAccessor>();

            _translationsProvider = translationsProvider;
        }

        public string UserName { get; set; }
        public bool EnableLocalLogin { get; set; } 
        public bool IsLocalAuthorization { get; set; }

        [BindProperty]
        public string ReturnUrl { get; set; }

        [BindProperty]
        public UserSignInSpec Input { get; set; }

        public bool SubmitSegmentAnonymousUserUuid { get; set; }

        public async Task OnGetAsync(string returnUrl = null)
        {            
            await PrepareModelAsync(returnUrl).ConfigureAwait(false);            
        }
        
        public async Task<IActionResult> OnPostAsync(string action = null)
        {
            await PrepareModelAsync(ReturnUrl).ConfigureAwait(false);

            if (!string.Equals(action, "submit"))
            {
                // the user clicked the "cancel" button

                if (IsLocalAuthorization)
                {
                    if (!string.IsNullOrEmpty(ReturnUrl))
                        return LocalRedirect(ReturnUrl);
                    else
                        return LocalRedirect("~/");
                }

                var context = await _interaction.GetAuthorizationContextAsync(ReturnUrl).ConfigureAwait(false);

                if (context != null)
                {
                    // If the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.

                    await _interaction.GrantConsentAsync(context, ConsentResponse.Denied).ConfigureAwait(false);

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
                UserModel user = await _identityServices.SignInUserAsync(Input).ConfigureAwait(false);

                PerformTracking(user);

                await _events.RaiseAsync(new UserLoginSuccessEvent(user.Email, user.Id, user.Email)).ConfigureAwait(false);
                
                if (IsLocalAuthorization)
                {
                    if (!string.IsNullOrEmpty(ReturnUrl))
                        return LocalRedirect(ReturnUrl);
                    else
                        LocalRedirect("~/");
                }

                if (_interaction.IsValidReturnUrl(ReturnUrl) || Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }

                return LocalRedirect("~/");                
            }
            catch (UnauthorizedApiException unauthorizedApiException)
            {
                if (Equals(unauthorizedApiException.GetErrorCode(), UnauthorizedApiException.EmailNotConfirmed))
                {
                    return RedirectToPage("./EmailNotConfirmed", new { UserUuid = unauthorizedApiException.UserUuid });
                }

                await _events.RaiseAsync(new UserLoginFailureEvent(Input.Email, "Invalid credentials")).ConfigureAwait(false);

                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(
                    unauthorizedApiException.GetErrorCode(), unauthorizedApiException.Message, unauthorizedApiException.Uuid, unauthorizedApiException.Name));
            }
            catch (ApiException e)
            {
                await _events.RaiseAsync(new UserLoginFailureEvent(Input.Email, "Invalid credentials")).ConfigureAwait(false);

                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));
            }

            await PrepareModelAsync(ReturnUrl).ConfigureAwait(false);

            return Page();            
        }

        private async Task PrepareModelAsync(string returnUrl)
        {
            if (_segmentProvider != null)
            {
                SubmitSegmentAnonymousUserUuid = true;
            }

            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = "~/";

            EnableLocalLogin = false;
            bool isLocalUrl = Url.IsLocalUrl(returnUrl);

            var context = await _interaction.GetAuthorizationContextAsync(returnUrl).ConfigureAwait(false);

            if (context == null && isLocalUrl)
            {
                IsLocalAuthorization = true;
                EnableLocalLogin = true;

                return;
            }

            if (context?.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId).ConfigureAwait(false);

                if (client != null)               
                    EnableLocalLogin = client.EnableLocalLogin;                                    
            }

            ReturnUrl = returnUrl;
            UserName = context?.LoginHint;
        }

        private void PerformTracking(UserModel user)
        {
            if (_segmentProvider != null)
            {
                var segmentClient = _segmentProvider.GetSegmentClient();

                if (!string.IsNullOrEmpty(Input.SegmentAnonymousUserUuid))
                {
                    string segmentAnonymousUserUuid = Input.SegmentAnonymousUserUuid;
                    segmentAnonymousUserUuid = segmentAnonymousUserUuid.Replace("%22", "");

                    segmentClient.Alias(segmentAnonymousUserUuid, user.Id);
                }

                segmentClient.Identify(user.Id, new Traits()
                    {
                        { "firstName", user.FirstName },
                        { "lastName", user.LastName },
                        { "createdAt", user.TermsAndConditionsAccepted?.ToString("o") },
                        { "email", user.Email }
                    });

                if (_tenantInfoAccessor != null)
                {
                    var tenantInfo = _tenantInfoAccessor.TenantInfo;

                    segmentClient.Track(user.Id, "Logged in", new Dictionary<string, object>()
                        {
                            { "developerName", tenantInfo?.DeveloperName },
                            { "tenantId", tenantInfo?.TenantUuid },
                            { "tenantName", tenantInfo?.Name }
                        });
                }
                else
                {
                    segmentClient.Track(user.Id, "Logged in");
                }
            }
        }
    }
}
