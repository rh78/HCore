using Microsoft.AspNetCore.Mvc;
using IdentityServer4.Services;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using HCore.Translations.Resources;
using System.Diagnostics;
using HCore.Identity.Attributes;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

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

        private readonly IDataProtectionProvider _dataProtectionProvider;

        public override string ModelAsJson { get =>
            JsonConvert.SerializeObject(
                new
                {
                    Error,
                    Description
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
        }
        
        public async Task OnGetAsync(string errorId, string errorCode, string errorDescription)
        {
            // check if we have identity error

            ShowRequestId = false;

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
    }
}