using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using HCore.Identity.Models;
using HCore.Web.Exceptions;
using HCore.Identity.Attributes;
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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class LoginModel : BasePageModelProvidingJsonModelData
    {
        private readonly TimeSpan SecondTimeSpan = TimeSpan.FromSeconds(1);

        private readonly IIdentityServices _identityServices;

        private readonly IOpenIddictContextProvider _openIddictContextProvider;


        private readonly ISegmentProvider _segmentProvider;

        private readonly ITenantInfoAccessor _tenantInfoAccessor;
        private readonly ITenantDataProvider _tenantDataProvider;

        private readonly ITranslationsProvider _translationsProvider;

        private readonly IDataProtectionProvider _dataProtectionProvider;

        public override string ModelAsJson { get; } = "{}";

        public LoginModel(
            IIdentityServices identityServices,
            IOpenIddictContextProvider openIddictContextProvider,
            ITranslationsProvider translationsProvider,
            IDataProtectionProvider dataProtectionProvider,
            IServiceProvider serviceProvider)
        {
            _identityServices = identityServices;
            _openIddictContextProvider = openIddictContextProvider;

            _segmentProvider = serviceProvider.GetService<ISegmentProvider>();

            _tenantInfoAccessor = serviceProvider.GetService<ITenantInfoAccessor>();
            _tenantDataProvider = serviceProvider.GetService<ITenantDataProvider>();

            _translationsProvider = translationsProvider;

            _dataProtectionProvider = dataProtectionProvider;
        }

        public bool IsLocalAuthorization { get; set; }
        public bool ShowSsoLink { get; set; }

        [BindProperty]
        public string ReturnUrl { get; set; }

        [BindProperty]
        public UserSignInSpec Input { get; set; }

        public bool SubmitSegmentAnonymousUserUuid { get; set; }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null, string challenge = null, string id = null)
        {            
            await PrepareModelAsync(returnUrl).ConfigureAwait(false);

            if (ShowSsoLink && string.Equals(challenge, "true"))
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                if (tenantInfo != null)
                {
                    string externalAuthenticationMethod = tenantInfo.ExternalAuthenticationMethod;

                    if (string.Equals(externalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodOidc) ||
                        string.Equals(externalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodSaml))
                    {
                        return await ChallengeExternalAsync(externalAuthenticationMethod, tenantInfo.OidcUseStateRedirect, tenantInfo.OidcStateRedirectUrl, tenantInfo.TenantUuid, targetTenantUuidString: id).ConfigureAwait(false);
                    }
                }
            }

            if (!ShowSsoLink && _tenantInfoAccessor != null)
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                if (tenantInfo != null)
                {
                    string externalAuthenticationMethod = tenantInfo.ExternalAuthenticationMethod;

                    if (string.Equals(externalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodOidc) ||
                        string.Equals(externalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodSaml))
                    {
                        return await ChallengeExternalAsync(externalAuthenticationMethod, tenantInfo.OidcUseStateRedirect, tenantInfo.OidcStateRedirectUrl, tenantInfo.TenantUuid, targetTenantUuidString: id).ConfigureAwait(false);
                    }
                }
            }

            return Page();
        }

#pragma warning disable CS1998 // This async method is lacking any "await" operator on purpose, thus it is called synchronous.
        private async Task<IActionResult> ChallengeExternalAsync(string externalAuthenticationMethod, bool oidcUseStateRedirect, string oidcStateRedirectUrl, long currentTenantUuid, string targetTenantUuidString)
#pragma warning restore CS1998 // This async method is lacking any "await" operator on purpose, thus it is called synchronous.
        {
            // initiate roundtrip to external authentication provider

            if (string.IsNullOrEmpty(ReturnUrl))
                ReturnUrl = "~/";

            if (!_openIddictContextProvider.IsValidReturnUrl(ReturnUrl, Url) && !Url.IsLocalUrl(ReturnUrl))
            { 
                throw new RequestFailedApiException(RequestFailedApiException.RedirectUrlInvalid, $"The redirect URL is invalid");
            }

            // start challenge and roundtrip the return URL and scheme 

            var items = new Dictionary<string, string?>() {
                { "returnUrl", ReturnUrl }
            };

            if (string.Equals(externalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodOidc))
            {
                if (oidcUseStateRedirect)
                {
                    if (string.IsNullOrEmpty(targetTenantUuidString) || !long.TryParse(targetTenantUuidString, out long targetTenantUuid))
                    {
                        var url = new Uri(oidcStateRedirectUrl);

                        var host = url.Host;
                        if (url.Port != 443)
                        {
                            host = $"{host}:{url.Port}";
                        }

                        oidcStateRedirectUrl = Url.Page("./Login", pageHandler: null, values: new
                        {
                            ReturnUrl,
                            Id = currentTenantUuid
                        }, protocol: "https", host: host);

                        return Redirect(oidcStateRedirectUrl);
                    }

                    items["targetTenantUuid"] = $"{targetTenantUuid}";
                }

                var props = new AuthenticationProperties(items)
                {
                    RedirectUri = Url.Page("./Login", pageHandler: "ExternalCallback", values: new { ReturnUrl }),
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

                var authenticateResult = await HttpContext.AuthenticateAsync(IdentityCoreConstants.ExternalOpenIddictScheme);

                if (authenticateResult?.Succeeded != true)
                    throw new Exception("The external authentication failed");

                // Delete temporary cookie used during external authentication

                await HttpContext.SignOutAsync(IdentityCoreConstants.ExternalOpenIddictScheme);

                // retrieve return URL

                var returnUrl = authenticateResult.Properties.GetString("returnUrl");
                if (string.IsNullOrEmpty(returnUrl))
                    returnUrl = "~/";

                await PrepareModelAsync(returnUrl).ConfigureAwait(false);

                if (string.IsNullOrEmpty(ReturnUrl))
                    ReturnUrl = "~/";

                if (!_openIddictContextProvider.IsValidReturnUrl(ReturnUrl, Url) && !Url.IsLocalUrl(ReturnUrl))
                {
                    throw new RequestFailedApiException(RequestFailedApiException.RedirectUrlInvalid, $"The redirect URL is invalid");
                }

                var (user, wasCreated) = await _identityServices.SignInUserAsync(authenticateResult).ConfigureAwait(false);

                PerformTracking(user);

                if (wasCreated)
                {
                    // make sure we continue on THIS tenant when completing the login
                    // this avoids breaking wizards etc. by redirecting to another tenant 
                    // that was selected before

                    Response.Cookies.Delete(TenantModel.CookieName);
                }

                var tenantInfoLocal = _tenantInfoAccessor.TenantInfo;

                var targetTenantUuid = GetTargetTenantUuid(authenticateResult);

                if (!tenantInfoLocal.OidcUseStateRedirect || targetTenantUuid == null)
                {
                    if (IsLocalAuthorization)
                    {
                        if (!string.IsNullOrEmpty(ReturnUrl))
                            return LocalRedirect(ReturnUrl);
                        else
                            LocalRedirect("~/");
                    }

                    if (_openIddictContextProvider.IsValidReturnUrl(ReturnUrl, Url) || Url.IsLocalUrl(ReturnUrl))
                    {
                        return Redirect(ReturnUrl);
                    }

                    return LocalRedirect("~/");
                }
                else
                {
                    var tenantInfo = await _tenantDataProvider.GetTenantByUuidThrowAsync(developerUuid: tenantInfoLocal.DeveloperUuid, targetTenantUuid.Value).ConfigureAwait(false);

                    var webUrl = tenantInfo.WebUrl;

                    if (!string.IsNullOrEmpty(webUrl) && webUrl.EndsWith("/"))
                    {
                        webUrl = webUrl[..(webUrl.Length - 1)];
                    }

                    if (IsLocalAuthorization)
                    {
                        if (!string.IsNullOrEmpty(ReturnUrl))
                            return Redirect($"{webUrl}{ReturnUrl}");
                        else
                            Redirect(tenantInfo.WebUrl);
                    }

                    if (_openIddictContextProvider.IsValidReturnUrl(ReturnUrl, Url) || Url.IsLocalUrl(ReturnUrl))
                    {
                        return Redirect($"{webUrl}{ReturnUrl}");
                    }

                    return Redirect(tenantInfo.WebUrl);
                }                
            }
            catch (UnauthorizedApiException unauthorizedApiException)
            {
                if (Equals(unauthorizedApiException.GetErrorCode(), UnauthorizedApiException.EmailNotConfirmed))
                {
                    var protectedUserUuid = _dataProtectionProvider.CreateProtector(nameof(EmailNotConfirmedModel)).Protect(unauthorizedApiException.UserUuid);

                    return RedirectToPage("./EmailNotConfirmed", new { UserUuid = protectedUserUuid });
                }

                throw;
            }
            catch (ApiException)
            {
                throw;
            }
        }

        private long? GetTargetTenantUuid(AuthenticateResult authenticateResult)
        {
            var items = authenticateResult?.Properties?.Items;
            
            if (items == null)
            {
                return null;
            }

            if (!authenticateResult.Properties.Items.TryGetValue("targetTenantUuid", out string targetTenantUuidString))
            {
                return null;
            }
            
            if (string.IsNullOrEmpty(targetTenantUuidString) || !long.TryParse(targetTenantUuidString, out long targetTenantUuid))
            {
                return null;
            }

            return targetTenantUuid;
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

                var openIddictContextModel = await _openIddictContextProvider.GetOpenIddictContextAsync(ReturnUrl, Url).ConfigureAwait(false);

                if (openIddictContextModel != null)
                {
                    /* TODO OpenIddict
                     * 
                    // If the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied external error response to the client.

                    await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied).ConfigureAwait(false);

                    */

                    // We can trust model.ReturnUrl since GetOpenIddictContextAsync returned non-null

                    return Redirect(ReturnUrl);
                }
                else
                {
                    // Since we don't have a valid context, then we just go back to the home page

                    return LocalRedirect("~/");
                }
            }

            ModelState.Clear();

            var beforeAuthorization = DateTimeOffset.Now;

            try
            {
                UserModel user = await _identityServices.SignInUserAsync(Input).ConfigureAwait(false);

                PerformTracking(user);

                if (IsLocalAuthorization)
                {
                    if (!string.IsNullOrEmpty(ReturnUrl))
                        return LocalRedirect(ReturnUrl);
                    else
                        LocalRedirect("~/");
                }

                if (_openIddictContextProvider.IsValidReturnUrl(ReturnUrl, Url) || Url.IsLocalUrl(ReturnUrl))
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

                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(
                    unauthorizedApiException.GetErrorCode(), unauthorizedApiException.Message, unauthorizedApiException.Uuid, unauthorizedApiException.Name));
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));
            }

            var afterAuthorization = DateTimeOffset.Now;

            var authorizationDurationTimespan = afterAuthorization - beforeAuthorization;

            if (authorizationDurationTimespan < SecondTimeSpan)
            {
                // make side channel attack when guessing email addresses impossible
                // see pentest report from Slashsec, 10/28/2024

                // whenever auth fails, we will always wait exactly 1 second

                await Task.Delay(SecondTimeSpan - authorizationDurationTimespan).ConfigureAwait(false);
            }

            await PrepareModelAsync(ReturnUrl).ConfigureAwait(false);

            return Page();            
        }

        private async Task PrepareModelAsync(string returnUrl)
        {
            if (_tenantInfoAccessor != null)
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                if (tenantInfo != null)
                {
                    ShowSsoLink = tenantInfo.ExternalAuthenticationAllowLocalLogin;
                }
            }

            if (_segmentProvider != null)
            {
                SubmitSegmentAnonymousUserUuid = true;
            }

            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = "~/";

            bool isLocalUrl = Url.IsLocalUrl(returnUrl);

            var openIddictContextModel = await _openIddictContextProvider.GetOpenIddictContextAsync(returnUrl, Url).ConfigureAwait(false);

            if (openIddictContextModel == null && isLocalUrl)
            {
                IsLocalAuthorization = true;

                ReturnUrl = returnUrl;

                return;
            }

            ReturnUrl = returnUrl;
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
    }
}
