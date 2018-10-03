using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using IdentityServer4.Services;
using System.Threading.Tasks;
using IdentityServer4.Models;
using ReinhardHolzner.Core.Identity.Attributes;

namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class ErrorModel : PageModel
    {
        public ErrorMessage Error { get; set; }

        private readonly IIdentityServerInteractionService _interaction;

        public ErrorModel(IIdentityServerInteractionService interaction)
        {
            _interaction = interaction;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task OnGet(string errorId)
        {
            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId).ConfigureAwait(false);

            if (message != null)
            {
                Error = message;
            }            
        }
    }
}