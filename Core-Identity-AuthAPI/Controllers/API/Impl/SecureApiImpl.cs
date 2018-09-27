using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ReinhardHolzner.Core.Identity.AuthAPI.Generated.Controllers;
using ReinhardHolzner.Core.Identity.AuthAPI.Generated.Models;
using ReinhardHolzner.Core.Templating.Emails;
using ReinhardHolzner.Core.Templating.Emails.ViewModels;
using ReinhardHolzner.Core.Web.Exceptions;

namespace ReinhardHolzner.Core.Identity.AuthAPI.Controllers.API.Impl
{
    public class SecureApiImpl : ApiImpl, ISecureApiController
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<SecureApiImpl> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateProvider _emailTemplateProvider;
        private readonly IUrlHelper _urlHelper;

        private readonly IdentityDbContext _identityDbContext;

        public SecureApiImpl(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<SecureApiImpl> logger,
            IEmailSender emailSender,
            IEmailTemplateProvider emailTemplateProvider,
            IUrlHelper urlHelper,
            IdentityDbContext identityDbContext,
            ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _emailTemplateProvider = emailTemplateProvider;
            _urlHelper = urlHelper;
            _identityDbContext = identityDbContext;
        }

        public async Task RegisterUserAsync([FromBody] UserSpec userSpec, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateModel(userSpec);

            try {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = new IdentityUser { UserName = userSpec.Email, Email = userSpec.Email };
                    var result = await _userManager.CreateAsync(user, userSpec.Password).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password");

                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);

                        var callbackUrl = _urlHelper.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { userId = user.Id, code },
                            protocol: "https");

                        EmailTemplate emailTemplate = await _emailTemplateProvider.GetConfirmAccountEmailAsync(
                            new ConfirmAccountEmailViewModel(callbackUrl)).ConfigureAwait(false);

                        await _emailSender.SendEmailAsync(userSpec.Email, emailTemplate.Subject, emailTemplate.Body).ConfigureAwait(false);

                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();

                        await _signInManager.SignInAsync(user, isPersistent: false).ConfigureAwait(false);
                    }
                    else
                    {
                        HandleIdentityError(result.Errors);
                    }
                }            
            } catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when registering user: {e}");

                throw new InternalServerErrorApiException();
            }            
        }

        public async Task SignOutUserAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await _signInManager.SignOutAsync().ConfigureAwait(false);

            _logger.LogInformation("User logged out");
        }
    }
}
