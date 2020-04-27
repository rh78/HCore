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
using HCore.Tenants;
using IdentityServer4;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Linq;
using HCore.Translations.Resources;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [ServiceFilter(typeof(SecurityHeadersAttribute))]
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

        private readonly IDataProtectionProvider _dataProtectionProvider;

        public string ValidationErrors { get =>
            JsonConvert.SerializeObject(
                GetValidationErrors(), 
                new JsonSerializerSettings()
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                });
            }

        public LoginModel(
            IIdentityServices identityServices,
            IConfigurationProvider configurationProvider,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            ITranslationsProvider translationsProvider,
            IDataProtectionProvider dataProtectionProvider,
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

            _dataProtectionProvider = dataProtectionProvider;
        }

        public string UserName { get; set; }
        public bool EnableLocalLogin { get; set; } 
        public bool IsLocalAuthorization { get; set; }

        [BindProperty]
        public string ReturnUrl { get; set; }

        [BindProperty]
        public UserSignInSpec Input { get; set; }

        public bool SubmitSegmentAnonymousUserUuid { get; set; }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {            
            await PrepareModelAsync(returnUrl).ConfigureAwait(false);

            if (_tenantInfoAccessor != null)
            {
                string externalAuthenticationMethod = _tenantInfoAccessor.TenantInfo?.ExternalAuthenticationMethod;

                if (string.Equals(externalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodOidc) ||
                    string.Equals(externalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodSaml))
                {
                    return await ChallengeExternalAsync(externalAuthenticationMethod).ConfigureAwait(false);
                }
            }

            return Page();
        }

#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        private async Task<IActionResult> ChallengeExternalAsync(string externalAuthenticationMethod)
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            // initiate roundtrip to external authentication provider

            if (string.IsNullOrEmpty(ReturnUrl))
                ReturnUrl = "~/";

            if (!_interaction.IsValidReturnUrl(ReturnUrl) && !Url.IsLocalUrl(ReturnUrl))
                throw new RequestFailedApiException(RequestFailedApiException.RedirectUrlInvalid, $"The redirect URL is invalid");

            // start challenge and roundtrip the return URL and scheme 

            if (string.Equals(externalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodOidc))
            {
                var props = new AuthenticationProperties
                {
                    RedirectUri = Url.Page("./Login", pageHandler: "ExternalCallback", values: new { ReturnUrl }),
                    Items =
                {
                    { "returnUrl", ReturnUrl }
                }
                };

                return Challenge(props, IdentityCoreConstants.ExternalOidcScheme);
            }
            else if (string.Equals(externalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodSaml))
            {
                var props = new AuthenticationProperties
                {
                    RedirectUri = Url.Page("./Login", pageHandler: "ExternalCallback", values: new { ReturnUrl }),
                    Items =
                    {
                        { "returnUrl", ReturnUrl }
                    }
                };

                return Challenge(props, IdentityCoreConstants.ExternalSamlScheme);
            }
            else
                throw new NotImplementedException();
        }

        public async Task<IActionResult> OnGetExternalCallbackAsync()
        {
            // Post processing of external authentication

            try
            {
                // Read external identity from the temporary cookie

                var authenticateResult = await HttpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

                if (authenticateResult?.Succeeded != true)
                    throw new Exception("The external authentication failed");

                // Delete temporary cookie used during external authentication

                await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

                // retrieve return URL

                var returnUrl = authenticateResult.Properties.GetString("returnUrl");
                if (string.IsNullOrEmpty(returnUrl))
                    returnUrl = "~/";

                await PrepareModelAsync(returnUrl).ConfigureAwait(false);

                if (string.IsNullOrEmpty(ReturnUrl))
                    ReturnUrl = "~/";

                if (!_interaction.IsValidReturnUrl(ReturnUrl) && !Url.IsLocalUrl(ReturnUrl))
                    throw new RequestFailedApiException(RequestFailedApiException.RedirectUrlInvalid, $"The redirect URL is invalid");

                UserModel user = await _identityServices.SignInUserAsync(authenticateResult).ConfigureAwait(false);

                PerformTracking(user);

                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.GetEmail())).ConfigureAwait(false);

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
                    var protectedUserUuid = _dataProtectionProvider.CreateProtector(nameof(EmailNotConfirmedModel)).Protect(unauthorizedApiException.UserUuid);

                    return RedirectToPage("./EmailNotConfirmed", new { UserUuid = protectedUserUuid });
                }

                await _events.RaiseAsync(new UserLoginFailureEvent(null, "Invalid credentials")).ConfigureAwait(false);

                throw;
            }
            catch (ApiException)
            {
                await _events.RaiseAsync(new UserLoginFailureEvent(null, "Invalid credentials")).ConfigureAwait(false);

                throw;
            }
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
                    // this will send back an access denied external error response to the client.

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

                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.GetEmail())).ConfigureAwait(false);

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
                    var protectedUserUuid = _dataProtectionProvider.CreateProtector(nameof(EmailNotConfirmedModel)).Protect(unauthorizedApiException.UserUuid);

                    return RedirectToPage("./EmailNotConfirmed", new { UserUuid = protectedUserUuid });
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

                ReturnUrl = returnUrl;

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

                if (Input != null && !string.IsNullOrEmpty(Input.SegmentAnonymousUserUuid))
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
                        { "email", user.GetEmail() }
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

        private List<string> GetValidationErrors()
        {
            var result = new List<string>();

            foreach (var value in ModelState.Values)
            {
                if (value.Errors != null)
                {
                    foreach (var error in value.Errors)
                    {
                        if (!string.IsNullOrEmpty(error.ErrorMessage))
                            result.Add(error.ErrorMessage);
                        else if (error.Exception != null)
                            result.Add(Messages.internal_server_error);
                    }
                }
            }
            
            return result;
        }
    }
}
