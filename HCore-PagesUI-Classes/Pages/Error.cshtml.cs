using Microsoft.AspNetCore.Mvc;
using Duende.IdentityServer.Services;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using HCore.Translations.Resources;
using System.Diagnostics;
using HCore.Identity.Attributes;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;
using HCore.Tenants.Providers;
using System.Web;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HCore.PagesUI.Classes.Pages
{
    [SecurityHeaders]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorModel : BasePageModelProvidingJsonModelData
    {
        private readonly IIdentityServerInteractionService _interaction;

        public bool ShowRequestId { get; set; }

        public string RequestId { get; set; }
        public string Error { get; set; }
        public string Description { get; set; }
        public bool ShowDescriptionOnly { get; set; }
        public bool AllowDescriptionHtml { get; set; }

        private readonly IDataProtectionProvider _dataProtectionProvider;

        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        public override string ModelAsJson { get =>
            JsonConvert.SerializeObject(
                new
                {
                    Error,
                    Description,
                    ShowDescriptionOnly,
                    AllowDescriptionHtml
                }, new JsonSerializerSettings()
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                }
            );
        }

        public ErrorModel(
            IDataProtectionProvider dataProtectionProvider,
            IServiceProvider serviceProvider)
        {
            _dataProtectionProvider = dataProtectionProvider;

            _interaction = serviceProvider.GetService<IIdentityServerInteractionService>();

            _tenantInfoAccessor = serviceProvider.GetService<ITenantInfoAccessor>();
        }
        
        public async Task OnGetAsync(string errorId, string errorCode, string errorDescription)
        {
            // check if we have identity error

            ShowRequestId = false;

            Description = null;
            ShowDescriptionOnly = false;
            AllowDescriptionHtml = false;

            if (_interaction != null && !string.IsNullOrEmpty(errorId))
            {
                // retrieve error details from identity server

                var message = await _interaction.GetErrorContextAsync(errorId).ConfigureAwait(false);

                if (message != null)
                {
                    RequestId = message.RequestId;
                    Error = message.Error;
                    Description = message.ErrorDescription;

                    if (!string.IsNullOrEmpty(Description) &&
                        !Description.EndsWith("."))
                    {
                        Description = $"{Description}.";
                    }

                    ShowRequestId = true;

                    return;
                }
            }

            // check if we have other error

            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            if (!string.IsNullOrEmpty(errorCode))
            {
                if (string.Equals(errorCode, "page_not_found"))
                {
                    errorDescription = $"{Messages.page_not_found}.";
                }
                else if (string.Equals(errorCode, "permission_denied"))
                {
                    errorDescription = $"{Messages.permission_denied}.";

                    var tenantInfo = _tenantInfoAccessor?.TenantInfo;

                    if (tenantInfo != null && !string.IsNullOrEmpty(tenantInfo.SupportEmail))
                    {
                        var htmlEncodedSupportEmailAddress = HttpUtility.HtmlEncode(tenantInfo.SupportEmail);
                        var permissionDeniedSupportMessage = tenantInfo.PermissionDeniedSupportMessage;

                        var localizedPermissionDeniedSupportMessage = ResolveLocalizedString(permissionDeniedSupportMessage);

                        if (!string.IsNullOrEmpty(localizedPermissionDeniedSupportMessage))
                        {
                            localizedPermissionDeniedSupportMessage = localizedPermissionDeniedSupportMessage.Replace("[SUPPORT EMAIL]", $"<a href='mailto:{htmlEncodedSupportEmailAddress}' class='support_email_link'>{htmlEncodedSupportEmailAddress}</a>", StringComparison.OrdinalIgnoreCase);
                            localizedPermissionDeniedSupportMessage = localizedPermissionDeniedSupportMessage.Replace("{supportEmailLink}", $"<a href='mailto:{htmlEncodedSupportEmailAddress}' class='support_email_link'>{htmlEncodedSupportEmailAddress}</a>", StringComparison.OrdinalIgnoreCase);

                            localizedPermissionDeniedSupportMessage = localizedPermissionDeniedSupportMessage.Replace("[KUNDENDIENST EMAIL]", $"<a href='mailto:{htmlEncodedSupportEmailAddress}' class='support_email_link'>{htmlEncodedSupportEmailAddress}</a>", StringComparison.OrdinalIgnoreCase);
                            localizedPermissionDeniedSupportMessage = localizedPermissionDeniedSupportMessage.Replace("{kundendienstEmailLink}", $"<a href='mailto:{htmlEncodedSupportEmailAddress}' class='support_email_link'>{htmlEncodedSupportEmailAddress}</a>", StringComparison.OrdinalIgnoreCase);

                            Description = localizedPermissionDeniedSupportMessage;

                            ShowDescriptionOnly = true;
                            AllowDescriptionHtml = true;
                        }
                    }
                }
                else if (string.Equals(errorCode, "ie11_and_lower_not_supported"))
                {
                    errorDescription = $"{Messages.ie11_and_lower_not_supported}.";
                }
                else if (string.Equals(errorCode, "maintenance_mode"))
                {
                    errorDescription = $"{Messages.maintenance_mode}.";
                }
                else if (!string.IsNullOrEmpty(errorDescription))
                {
                    try
                    {
                        errorDescription = _dataProtectionProvider.CreateProtector("Error").Unprotect(errorDescription);
                    }
                    catch (Exception)
                    {
                        errorDescription = $"{Messages.error_message_is_invalid}.";
                    }
                }

                Error = errorDescription;

                if (!string.IsNullOrEmpty(Error) &&
                        !Error.EndsWith("."))
                {
                    Error = $"{Error}.";
                }

                return;
            }

            Error = $"{Messages.internal_server_error}.";

            ShowRequestId = true;
        }

        private string ResolveLocalizedString(Dictionary<string, string> localizedStrings)
        {
            if (localizedStrings == null || localizedStrings.Count == 0)
                return null;

            string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            var localizedString = localizedStrings
                .Where(localizedString => string.Equals(localizedString.Key, culture))
                .Select(localizedString => localizedString.Value)
                .FirstOrDefault();

            if (localizedString != null)
                return localizedString;

            localizedString = localizedStrings
                .Where(localizedString => string.Equals(localizedString.Key, "x-default"))
                .Select(localizedString => localizedString.Value)
                .FirstOrDefault();

            if (localizedString != null)
                return localizedString;

            localizedString = localizedStrings
                .Where(localizedString => string.Equals(localizedString.Key, "en"))
                .Select(localizedString => localizedString.Value)
                .FirstOrDefault();

            if (localizedString != null)
                return localizedString;

            return localizedStrings
                .Select(localizedString => localizedString.Value)
                .First();
        }
    }
}