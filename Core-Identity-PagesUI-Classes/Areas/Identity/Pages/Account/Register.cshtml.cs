using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ReinhardHolzner.Core.Templating.Emails;
using ReinhardHolzner.Core.Templating.Emails.ViewModels;

namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateProvider _emailTemplateProvider;

        private readonly IdentityDbContext _identityDbContext;
        
        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IEmailTemplateProvider emailTemplateProvider,
            IdentityDbContext identityDbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _emailTemplateProvider = emailTemplateProvider;
            _identityDbContext = identityDbContext;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "E-Mail")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "Das {0} muss mindestens {2} und maximal {1} Zeichen lang sein.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Passwort")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Passwort bestätigen")]
            [Compare("Password", ErrorMessage = "Die Passwörter stimmen leider nicht überein.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (ModelState.IsValid)
            {
                try
                {
                    using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                    {
                        var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                        var result = await _userManager.CreateAsync(user, Input.Password);
                        if (result.Succeeded)
                        {
                            _logger.LogInformation("User created a new account with password");

                            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            var callbackUrl = Url.Page(
                                "/Account/ConfirmEmail",
                                pageHandler: null,
                                values: new { userId = user.Id, code },
                                protocol: Request.Scheme);

                            EmailTemplate emailTemplate = await _emailTemplateProvider.GetConfirmAccountEmailAsync(
                                new ConfirmAccountEmailViewModel(callbackUrl)).ConfigureAwait(false);

                            await _emailSender.SendEmailAsync(Input.Email, emailTemplate.Subject, emailTemplate.Body);

                            await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                            transaction.Commit();

                            await _signInManager.SignInAsync(user, isPersistent: false);

                            return LocalRedirect(returnUrl);
                        }
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                } catch (Exception e)
                {
                    _logger.LogError($"Error when registering user: {e}");

                    ModelState.AddModelError(string.Empty, "Es ist ein unerwarteter Fehler aufgetreten. Bitte versuchen Sie es noch einmal!");
                }
            }

            return Page();
        }
    }
}
