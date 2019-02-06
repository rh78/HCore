using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.Models;
using HCore.Web.Exceptions;
using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.Services;
using HCore.Identity.Providers;
using HCore.Tenants.Providers;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class RegisterModel : PageModel
    {
        private readonly IIdentityServices _identityServices;
        private readonly IConfigurationProvider _configurationProvider;        
        private readonly IEventService _events;

        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        public RegisterModel(
            IIdentityServices identityServices,
            IConfigurationProvider configurationProvider,            
            IEventService events,
            IServiceProvider serviceProvider)
        {
            _identityServices = identityServices;
            _configurationProvider = configurationProvider;
            
            _events = events;

            _tenantInfoAccessor = serviceProvider.GetService<ITenantInfoAccessor>();
        }

        [BindProperty]
        public UserSpec Input { get; set; }

        public string ProductName { get; set; }
        public string PrivacyPolicyUrl { get; set; }
        public string TermsAndConditionsUrl { get; set; }

        public void OnGet(string emailAddress = null)
        {
            ProcessGdprData();

            emailAddress = ProcessEmail(emailAddress);

            if (!string.IsNullOrEmpty(emailAddress))
            {
                Input = new UserSpec()
                {
                    Email = emailAddress
                };
            }
        }

        private string ProcessEmail(string emailAddress)
        {
            if (string.IsNullOrEmpty(emailAddress))
                return null;

            if (!new EmailAddressAttribute().IsValid(emailAddress))
                return null;

            if (emailAddress.Length > UserModel.MaxEmailAddressLength)
                return null;

            return emailAddress;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ProcessGdprData();

            ModelState.Clear();

            try
            {
                UserModel user = await _identityServices.CreateUserAsync(Input, isSelfRegistration: true).ConfigureAwait(false);

                if (_configurationProvider.RequireEmailConfirmed && !user.EmailConfirmed)
                {
                    return RedirectToPage("./EmailNotConfirmed", new { UserUuid = user.Id });
                }
                else
                {
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.Email, user.Id, user.Email)).ConfigureAwait(false);
                }

                return LocalRedirect("~/");
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
            }

            return Page();
        }

        private void ProcessGdprData()
        {
            ProductName = _configurationProvider.ProductName;
            PrivacyPolicyUrl = _configurationProvider.PrivacyPolicyUrl;
            TermsAndConditionsUrl = _configurationProvider.TermsAndConditionsUrl;

            if (_tenantInfoAccessor != null)
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                if (tenantInfo != null)
                {
                    string tenantProductName = tenantInfo.ProductName;
                    if (!string.IsNullOrEmpty(tenantProductName))
                        ProductName = tenantProductName;

                    string tenantPrivacyPolicyUrl = tenantInfo.DeveloperPrivacyPolicyUrl;
                    if (!string.IsNullOrEmpty(tenantPrivacyPolicyUrl))
                        PrivacyPolicyUrl = tenantPrivacyPolicyUrl;

                    string tenantTermsAndConditionsUrl = tenantInfo.DeveloperTermsAndConditionsUrl;
                    if (!string.IsNullOrEmpty(tenantTermsAndConditionsUrl))
                        TermsAndConditionsUrl = tenantTermsAndConditionsUrl;
                }
            }
        }
    }
}