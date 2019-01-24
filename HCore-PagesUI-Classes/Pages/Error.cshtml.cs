using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IdentityServer4.Services;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using HCore.Translations.Resources;
using System.Diagnostics;

namespace HCore.PagesUI.Classes.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorModel : PageModel
    {
        private readonly IIdentityServerInteractionService _interaction;

        public bool ShowRequestId { get; set; }

        public string RequestId { get; set; }
        public string Error { get; set; }
        public string Description { get; set; }

        public ErrorModel(IServiceProvider serviceProvider)
        {
            _interaction = serviceProvider.GetService<IIdentityServerInteractionService>();
        }
        
        public async Task OnGet(string errorId, string errorCode, string errorDescription)
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

                    ShowRequestId = true;

                    return;
                }
            }

            // check if we have other error

            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            if (!string.IsNullOrEmpty(errorCode))
            {
                Error = errorDescription;

                return;
            }

            Error = Messages.internal_server_error;

            ShowRequestId = true;
        }
    }
}