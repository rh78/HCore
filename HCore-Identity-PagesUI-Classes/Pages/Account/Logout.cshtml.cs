﻿using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc;
using HCore.Identity.Attributes;
using HCore.Identity.Services;
using Microsoft.AspNetCore.Http;
using System;
using HCore.Tenants.Providers;
using Newtonsoft.Json;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class LogoutModel : BasePageModelProvidingJsonModelData
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

        public string WebAddress { get; set; }
        public string PoweredByShort { get; set; }
        public bool HidePoweredBy { get; set; }

        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        public override string ModelAsJson { get =>
            JsonConvert.SerializeObject(
                new
                {
                    LoggedOut,
                    PostLogoutRedirectUri,
                    WebAddress,
                    PoweredByShort,
                    HidePoweredBy
                }, new JsonSerializerSettings()
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                }
            );
        }

        public LogoutModel(
             IIdentityServices identityServices,
             IIdentityServerInteractionService interaction,
             ITenantInfoAccessor tenantInfoAccessor,
             IEventService events)
        {
            _identityServices = identityServices;
            _interaction = interaction;
            _tenantInfoAccessor = tenantInfoAccessor;
            _events = events;
        }

        public async Task<IActionResult> OnGetAsync(string logoutId = null)
        {
            // Build a model so the logout page knows what to display

            await PrepareLogoutModelAsync(logoutId).ConfigureAwait(false);

            if (!ShowLogoutPrompt)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.

                LogoutId = logoutId;

                return await OnPostAsync().ConfigureAwait(false);
            }

            return Page();
        }

        private async Task PrepareLogoutModelAsync(string logoutId)
        {
            LogoutId = logoutId;
            ShowLogoutPrompt = true;

            var tenantInfo = _tenantInfoAccessor?.TenantInfo;

            if (tenantInfo != null)
            {
                WebAddress = tenantInfo.WebAddress;
                PoweredByShort = tenantInfo.PoweredByShort;

                HidePoweredBy = tenantInfo.HidePoweredBy;
            }

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page

                ShowLogoutPrompt = false;                

                return;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId).ConfigureAwait(false);

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

        public async Task<IActionResult> OnPostAsync()
        {
            // build a model so the logged out page knows what to display

            await PrepareLoggedOutModelAsync(LogoutId).ConfigureAwait(false);

            if (User?.Identity.IsAuthenticated == true)
            {
                // delete authentication cookies

                HandleTenantCookie();

                await _identityServices.SignOutUserAsync(HttpContext).ConfigureAwait(false);

                // raise the logout event
                
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName())).ConfigureAwait(false);
            }

            LoggedOut = true;

            return Page();
        }

        private void HandleTenantCookie()
        {
            if (_tenantInfoAccessor != null)
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                if (tenantInfo != null)
                {
                    Response.Cookies.Append(TenantModel.CookieName, "", new CookieOptions()
                    {
                        Domain = tenantInfo.DeveloperAuthCookieDomain,
                        Expires = DateTime.Now.AddDays(-1),
                        Secure = true,
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax
                    });
                }
            }
        }

        private async Task PrepareLoggedOutModelAsync(string logoutId)
        {
            AutomaticRedirectAfterSignOut = true;

            // get context information (client name, post logout redirect URI and iframe for federated signout)

            var context = await _interaction.GetLogoutContextAsync(logoutId).ConfigureAwait(false);

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