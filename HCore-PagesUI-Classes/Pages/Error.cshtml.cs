using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IdentityServer4.Services;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using HCore.Translations.Resources;

namespace HCore.PagesUI.Classes.Pages
{
    public class ErrorModel : PageModel
    {
        private readonly IIdentityServerInteractionService _interaction;

        public string RequestId { get; set; }
        public string Error { get; set; }
        public string Description { get; set; }

        public ErrorModel(IServiceProvider serviceProvider)
        {
            _interaction = serviceProvider.GetService<IIdentityServerInteractionService>();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task OnGet(string errorId, string errorCode, string errorDescription)
        {
            // check if we have identity error

            if (_interaction != null && !string.IsNullOrEmpty(errorId))
            {
                // retrieve error details from identity server

                var message = await _interaction.GetErrorContextAsync(errorId).ConfigureAwait(false);

                if (message != null)
                {
                    RequestId = message.RequestId;
                    Error = message.Error;
                    Description = message.ErrorDescription;

                    return;
                }
            }

            // check if we have other error

            RequestId = HttpContext.TraceIdentifier;

            if (!string.IsNullOrEmpty(errorCode))
            {
                Error = errorDescription;

                return;
            }

            Error = ErrorCodes.internal_server_error;
        }
    }
}