using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HCore.Identity.Attributes;
using HCore.Identity.Models;
using HCore.Web.Exceptions;
using HCore.Identity.Services;
using HCore.Translations.Providers;
using reCAPTCHA.AspNetCore;
using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class ForgotPasswordModel : BasePageModelProvidingJsonModelData
    {
        private readonly IIdentityServices _identityServices;
        private readonly ITranslationsProvider _translationsProvider;

        public ForgotPasswordModel(
            IIdentityServices identityServices,
            ITranslationsProvider translationsProvider,
            IServiceProvider serviceProvider)
        {
            _identityServices = identityServices;
            _translationsProvider = translationsProvider;

            var recaptchaSettings = serviceProvider.GetService<IOptions<RecaptchaSettings>>();

            if (recaptchaSettings != null)
            {
                Recaptcha = recaptchaSettings.Value;
            }
        }

        [BindProperty]
        public UserForgotPasswordSpec Input { get; set; }

        public RecaptchaSettings Recaptcha { get; set; }
        
        public override string Values { get =>
            JsonConvert.SerializeObject(
                new
                {
                    Email = Input.Email,
                    RecaptchaSiteKey = Recaptcha.SiteKey
                }, new JsonSerializerSettings()
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                }
            );
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            try
            {
                await _identityServices.UserForgotPasswordAsync(Input, request: Request).ConfigureAwait(false);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }
            catch (ApiException e)
            {
                ModelState.AddModelError(string.Empty, _translationsProvider.TranslateError(e.GetErrorCode(), e.Message, e.Uuid, e.Name));
            }

            return Page();            
        }
    }
}
