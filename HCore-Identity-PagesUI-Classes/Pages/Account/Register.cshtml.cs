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
using HCore.Segment.Providers;
using Segment.Model;
using System.Collections.Generic;
using HCore.Translations.Providers;
using System.Text.RegularExpressions;
using System.Net;
using System.Globalization;
using reCAPTCHA.AspNetCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;
using HCore.Translations.Resources;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class RegisterModel : PageModel
    {
        private readonly IIdentityServices _identityServices;
        private readonly IConfigurationProvider _configurationProvider;        
        private readonly IEventService _events;

        private readonly ISegmentProvider _segmentProvider;

        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        private readonly ITranslationsProvider _translationsProvider;

        private readonly IDataProtectionProvider _dataProtectionProvider;

        public string Values { get =>
            JsonConvert.SerializeObject(
                new
                {
                    RequiresTermsAndConditions,
                    ProductName,
                    TermsAndConditionsUrl,
                    PrivacyPolicyUrl,
                    RecaptchaSiteKey = Recaptcha.SiteKey
                }, new JsonSerializerSettings()
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                });
            }

        public string ValidationErrors { get =>
            JsonConvert.SerializeObject(
                GetValidationErrors(), 
                new JsonSerializerSettings()
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                });
            }

        public RegisterModel(
            IIdentityServices identityServices,
            IConfigurationProvider configurationProvider,            
            IEventService events,
            ITranslationsProvider translationsProvider,
            IDataProtectionProvider dataProtectionProvider,
            IServiceProvider serviceProvider)
        {
            _identityServices = identityServices;
            _configurationProvider = configurationProvider;
            
            _events = events;

            _segmentProvider = serviceProvider.GetService<ISegmentProvider>();

            _tenantInfoAccessor = serviceProvider.GetService<ITenantInfoAccessor>();

            _translationsProvider = translationsProvider;

            _dataProtectionProvider = dataProtectionProvider;

            var recaptchaSettings = serviceProvider.GetService<IOptions<RecaptchaSettings>>();

            if (recaptchaSettings != null)
            {
                Recaptcha = recaptchaSettings.Value;
            }
        }

        [BindProperty]
        public UserSpec Input { get; set; }

        public string ProductName { get; set; }
        public string PrivacyPolicyUrl { get; set; }
        public bool RequiresTermsAndConditions { get; set; }
        public string TermsAndConditionsUrl { get; set; }

        public bool SubmitSegmentAnonymousUserUuid { get; set; }

        public RecaptchaSettings Recaptcha { get; set; }

        public IActionResult OnGet(string emailAddress = null, string firstName = null, string lastName = null)
        {
            if (_tenantInfoAccessor != null)
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                if (tenantInfo.ExternalAuthenticationMethod != null)
                {
                    // do not allow local registration, if users are externally managed

                    return LocalRedirect("~/");
                }
            }

            PrepareModel();

            emailAddress = ProcessEmail(emailAddress);

            Input = new UserSpec();

            if (!string.IsNullOrEmpty(emailAddress))
            {
                Input.Email = emailAddress;
            }

            firstName = ProcessFirstName(firstName);

            if (!string.IsNullOrEmpty(firstName))
            {
                Input.FirstName = firstName;
            }

            lastName = ProcessLastName(lastName);

            if (!string.IsNullOrEmpty(lastName))
            {
                Input.LastName = lastName;
            }

            return Page();
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

        private string ProcessFirstName(string firstName)
        {
            if (string.IsNullOrEmpty(firstName))
                return null;

            if (firstName.Length > UserModel.MaxFirstNameLength)
                return null;

            firstName = CleanInput(firstName);            

            if (string.IsNullOrEmpty(firstName))
                return null;

            return firstName;
        }

        private string ProcessLastName(string lastName)
        {
            if (string.IsNullOrEmpty(lastName))
                return null;

            if (lastName.Length > UserModel.MaxLastNameLength)
                return null;

            lastName = CleanInput(lastName);

            if (string.IsNullOrEmpty(lastName))
                return null;

            return lastName;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            PrepareModel();

            ModelState.Clear();

            try
            {
                UserModel user = await _identityServices.CreateUserAsync(Input, isSelfRegistration: true, request: Request).ConfigureAwait(false);

                PerformTracking(user);

                if (_configurationProvider.RequireEmailConfirmed && !user.EmailConfirmed)
                {
                    var protectedUserUuid = _dataProtectionProvider.CreateProtector(nameof(EmailNotConfirmedModel)).Protect(user.Id);

                    return RedirectToPage("./EmailNotConfirmed", new { UserUuid = protectedUserUuid });
                }
                else
                {
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.GetEmail())).ConfigureAwait(false);
                }

                return LocalRedirect("~/");
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));
            }

            return Page();
        }

        private void PrepareModel()
        {
            if (_segmentProvider != null)
            {
                SubmitSegmentAnonymousUserUuid = true;
            }

            ProductName = _configurationProvider.ProductName;
            PrivacyPolicyUrl = _configurationProvider.PrivacyPolicyUrl;
            RequiresTermsAndConditions = _configurationProvider.RequiresTermsAndConditions;
            TermsAndConditionsUrl = _configurationProvider.TermsAndConditionsUrl;

            if (_tenantInfoAccessor != null)
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                if (tenantInfo != null)
                {
                    string developerName = tenantInfo.DeveloperName;
                    if (!string.IsNullOrEmpty(developerName))
                        ProductName = developerName;

                    string tenantPrivacyPolicyUrl = tenantInfo.DeveloperPrivacyPolicyUrl;
                    if (!string.IsNullOrEmpty(tenantPrivacyPolicyUrl))
                        PrivacyPolicyUrl = tenantPrivacyPolicyUrl;

                    RequiresTermsAndConditions = tenantInfo.RequiresTermsAndConditions;

                    if (RequiresTermsAndConditions)
                    {
                        string tenantTermsAndConditionsUrl = tenantInfo.DeveloperTermsAndConditionsUrl;
                        if (!string.IsNullOrEmpty(tenantTermsAndConditionsUrl))
                            TermsAndConditionsUrl = tenantTermsAndConditionsUrl;
                    }
                }
            }

            if (!string.IsNullOrEmpty(PrivacyPolicyUrl))
                PrivacyPolicyUrl = PrivacyPolicyUrl + $"?lang={CultureInfo.CurrentCulture.TwoLetterISOLanguageName}";

            if (!string.IsNullOrEmpty(TermsAndConditionsUrl))
                TermsAndConditionsUrl = TermsAndConditionsUrl + $"?lang={CultureInfo.CurrentCulture.TwoLetterISOLanguageName}";
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
                        { "createdAt", user.PrivacyPolicyAccepted?.ToString("o") },
                        { "email", user.GetEmail() }
                    });

                if (_tenantInfoAccessor != null)
                {
                    var tenantInfo = _tenantInfoAccessor.TenantInfo;

                    segmentClient.Track(user.Id, "Registered", new Dictionary<string, object>()
                        {
                            { "developerName", tenantInfo?.DeveloperName },
                            { "tenantId", tenantInfo?.TenantUuid },
                            { "tenantName", tenantInfo?.Name }
                        });
                }
                else
                {
                    segmentClient.Track(user.Id, "Registered");
                }
            }
        }

        // see https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-strip-invalid-characters-from-a-string

        private string CleanInput(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            // Replace invalid characters with empty strings.

            str = WebUtility.UrlDecode(str);

            try
            {
                return Regex.Replace(str, @"[^\w\.@\-\s]", "",
                                     RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters, 
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return null;
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