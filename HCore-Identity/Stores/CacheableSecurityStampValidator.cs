using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HCore.Identity.Stores
{
    public class CacheableSecurityStampValidator : SecurityStampValidator<UserModel>
    {
        private readonly ISecurityStampCacheProvider _securityStampCacheProvider;

        private readonly UserManager<UserModel> _userManager;

        private readonly IdentityOptions _options;

        public CacheableSecurityStampValidator(IServiceProvider serviceProvider, IOptions<SecurityStampValidatorOptions> options, SignInManager<UserModel> signInManager, UserManager<UserModel> userManager, IOptions<IdentityOptions> optionsAccessor, ILoggerFactory logger) : 
            base(options, signInManager, logger)
        {
            _securityStampCacheProvider = serviceProvider.GetService<ISecurityStampCacheProvider>();

            _userManager = userManager;

            _options = optionsAccessor?.Value ?? new IdentityOptions();
        }

        [Obsolete]
        public CacheableSecurityStampValidator(IServiceProvider serviceProvider, IOptions<SecurityStampValidatorOptions> options, SignInManager<UserModel> signInManager, UserManager<UserModel> userManager, ISystemClock clock, IOptions<IdentityOptions> optionsAccessor, ILoggerFactory logger) : 
            base(options, signInManager, clock, logger)
        {
            _securityStampCacheProvider = serviceProvider.GetService<ISecurityStampCacheProvider>();

            _userManager = userManager;

            _options = optionsAccessor?.Value ?? new IdentityOptions();
        }

        public override async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var principal = context.Principal;
            
            if (principal == null)
            {
                await base.ValidateAsync(context).ConfigureAwait(false);

                return;
            }

            var userId = _userManager.GetUserId(principal);

            if (string.IsNullOrEmpty(userId))
            {
                await base.ValidateAsync(context).ConfigureAwait(false);

                return;
            }

            var securityStampValue = principal?.FindFirstValue(_options.ClaimsIdentity.SecurityStampClaimType);

            if (string.IsNullOrEmpty(securityStampValue))
            {
                await base.ValidateAsync(context).ConfigureAwait(false);

                return;
            }

            var currentUtc = TimeProvider.GetUtcNow();
            var issuedUtc = context.Properties.IssuedUtc;

            // Only validate if enough time has elapsed
            var validate = (issuedUtc == null);

            if (issuedUtc != null)
            {
                var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                validate = timeElapsed > Options.ValidationInterval;
            }

            if (validate)
            {
                // if we anyways would validate, lets validate as usual

                await base.ValidateAsync(context).ConfigureAwait(false);

                return;
            }

            // in this case we use our cache

            var securityStamp = await _securityStampCacheProvider.GetSecurityStampAsync(userId).ConfigureAwait(false);

            if (string.IsNullOrEmpty(securityStamp))
            {
                // if we anyways would validate, lets validate as usual

                await base.ValidateAsync(context).ConfigureAwait(false);

                return;
            }

            if (!string.Equals(securityStamp, securityStampValue))
            {
                context.RejectPrincipal();
                await SignInManager.SignOutAsync();
                await SignInManager.Context.SignOutAsync(IdentityConstants.TwoFactorRememberMeScheme);
            }

            // otherwise, all is fine
        }
    }
}
