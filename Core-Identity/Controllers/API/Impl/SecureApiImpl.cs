using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ReinhardHolzner.Core.Identity.Generated.Controllers;
using ReinhardHolzner.Core.Identity.Generated.Models;
using ReinhardHolzner.Core.Identity.Database.SqlServer;
using ReinhardHolzner.Core.Identity.Database.SqlServer.Models.Impl;
using ReinhardHolzner.Core.Templating.Emails;
using ReinhardHolzner.Core.Templating.Emails.ViewModels;
using ReinhardHolzner.Core.Web.Exceptions;
using ReinhardHolzner.Core.Web.Result;

namespace ReinhardHolzner.Core.Identity.Controllers.API.Impl
{
    public class SecureApiImpl : ApiImpl, ISecureApiController
    {
        private readonly SignInManager<UserModel> _signInManager;
        private readonly UserManager<UserModel> _userManager;
        private readonly ILogger<SecureApiImpl> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateProvider _emailTemplateProvider;
        private readonly IUrlHelper _urlHelper;
        private readonly SqlServerIdentityDbContext _identityDbContext;

        public SecureApiImpl(
            UserManager<UserModel> userManager,
            SignInManager<UserModel> signInManager,
            ILogger<SecureApiImpl> logger,
            IEmailSender emailSender,
            IEmailTemplateProvider emailTemplateProvider,
            IUrlHelper urlHelper,
            SqlServerIdentityDbContext identityDbContext,
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

        public async Task<ApiResult<User>> CreateUserAsync([FromBody]UserSpec userSpec, CancellationToken cancellationToken = default(CancellationToken))
        {
            userSpec.Email = ProcessEmail(userSpec.Email, true);
            userSpec.Password = ProcessPassword(userSpec.Password);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = new UserModel { UserName = userSpec.Email, Email = userSpec.Email };

                    var result = await _userManager.CreateAsync(user, userSpec.Password).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password");

                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);

                        var currentCultureInfo = Thread.CurrentThread.CurrentUICulture;

                        var callbackUrl = _urlHelper.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { userUuid = user.Id, code, culture = currentCultureInfo.ToString() },
                            protocol: "https");

                        EmailTemplate emailTemplate = await _emailTemplateProvider.GetConfirmAccountEmailAsync(
                            new ConfirmAccountEmailViewModel(callbackUrl), currentCultureInfo).ConfigureAwait(false);

                        await _emailSender.SendEmailAsync(userSpec.Email, emailTemplate.Subject, emailTemplate.Body).ConfigureAwait(false);

                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();

                        await _signInManager.SignInAsync(user, isPersistent: false).ConfigureAwait(false);

                        return new ApiResult<User>(new User()
                        {
                            Uuid = user.Id,
                            Email = user.Email,
                            EmailConfirmed = user.EmailConfirmed,
                            PhoneNumber = user.PhoneNumber,
                            PhoneNumberConfirmed = user.PhoneNumberConfirmed
                        });
                    }
                    else
                    {
                        HandleIdentityError(result.Errors);
                        
                        // to satisfy the compiler
                        throw new InternalServerErrorApiException();
                    }
                }
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when registering user: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task<ApiResult<User>> SignInUserAsync([FromBody] UserSignInSpec userSignInSpec, CancellationToken cancellationToken = default(CancellationToken))
        {
            userSignInSpec.Email = ProcessEmail(userSignInSpec.Email, true);
            userSignInSpec.Password = ProcessPassword(userSignInSpec.Password);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    bool remember = userSignInSpec.RememberSet && userSignInSpec.Remember;

                    var result = await _signInManager.PasswordSignInAsync(userSignInSpec.Email, userSignInSpec.Password, remember, lockoutOnFailure: true).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User signed in");

                        var user = await _userManager.FindByEmailAsync(userSignInSpec.Email).ConfigureAwait(false);

                        if (user == null)
                        {
                            // This should not happen as we authorized

                            _logger.LogError("Invalid state reached when signing in user");

                            throw new InternalServerErrorApiException();
                        }

                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();

                        return new ApiResult<User>(new User()
                        {
                            Uuid = user.Id,
                            Email = user.Email,
                            EmailConfirmed = user.EmailConfirmed,
                            PhoneNumber = user.PhoneNumber,
                            PhoneNumberConfirmed = user.PhoneNumberConfirmed
                        });
                    }

                    // authorization failed 

                    await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                    transaction.Commit();

                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account is locked out");

                        throw new UnauthorizedApiException(UnauthorizedApiException.AccountLockedOut, "The user account is locked out");
                    }
                    else
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.InvalidCredentials, "The user credentials are not valid");
                    }
                }
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when signing in user: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task ConfirmUserEmailAddressAsync([FromRoute][Required]string userUuid, [FromBody]UserConfirmEmailSpec userConfirmEmailSpec, CancellationToken cancellationToken = default(CancellationToken))
        {
            userUuid = ProcessUserUuid(userUuid);

            userConfirmEmailSpec.Code = ProcessCode(userConfirmEmailSpec.Code);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                    if (user == null)
                    {
                        throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found");
                    }

                    var result = await _userManager.ConfirmEmailAsync(user, userConfirmEmailSpec.Code).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();
                    }
                    else
                    {
                        HandleIdentityError(result.Errors);
                    }
                }
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when confirming user email address: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task UserForgotPasswordAsync([FromBody] UserForgotPasswordSpec userForgotPasswordSpec, CancellationToken cancellationToken = default(CancellationToken))
        {
            userForgotPasswordSpec.Email = ProcessEmail(userForgotPasswordSpec.Email, true);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = await _userManager.FindByEmailAsync(userForgotPasswordSpec.Email).ConfigureAwait(false);

                    if (user == null || !await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false))
                    {
                        // Don't reveal that the user does not exist or is not confirmed
                        return;
                    }

                    var code = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);

                    var currentCultureInfo = Thread.CurrentThread.CurrentUICulture;

                    var callbackUrl = _urlHelper.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { code, culture = currentCultureInfo.ToString() },
                        protocol: "https");

                    EmailTemplate emailTemplate = await _emailTemplateProvider.GetForgotPasswordEmailAsync(
                            new ForgotPasswordEmailViewModel(callbackUrl), currentCultureInfo).ConfigureAwait(false);

                    await _emailSender.SendEmailAsync(userForgotPasswordSpec.Email, emailTemplate.Subject, emailTemplate.Body).ConfigureAwait(false);

                    await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                    transaction.Commit();
                }
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when initiating user 'forgot password' flow: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task ResetUserPasswordAsync([FromBody] ResetUserPasswordSpec resetUserPasswordSpec, CancellationToken cancellationToken = default(CancellationToken))
        {
            resetUserPasswordSpec.Email = ProcessEmail(resetUserPasswordSpec.Email, true);
            resetUserPasswordSpec.Password = ProcessPassword(resetUserPasswordSpec.Password);
            resetUserPasswordSpec.PasswordConfirmation = ProcessPasswordConfirmation(resetUserPasswordSpec.Password, resetUserPasswordSpec.PasswordConfirmation);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = await _userManager.FindByEmailAsync(resetUserPasswordSpec.Email).ConfigureAwait(false);

                    if (user == null)
                    {
                        // Don't reveal that the user does not exist
                        return;
                    }

                    var result = await _userManager.ResetPasswordAsync(user, resetUserPasswordSpec.Code, resetUserPasswordSpec.Password).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();
                    }
                    else
                    {
                        HandleIdentityError(result.Errors);
                    }
                }
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when resetting user password: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task SetUserPasswordAsync([FromRoute, Required] string userUuid, [FromBody] SetUserPasswordSpec setUserPasswordSpec, CancellationToken cancellationToken = default(CancellationToken))
        {
            userUuid = ProcessUserUuid(userUuid);

            setUserPasswordSpec.OldPassword = ProcessPassword(setUserPasswordSpec.OldPassword);
            setUserPasswordSpec.NewPassword = ProcessPassword(setUserPasswordSpec.NewPassword);
            setUserPasswordSpec.NewPasswordConfirmation = ProcessPasswordConfirmation(setUserPasswordSpec.NewPassword, setUserPasswordSpec.NewPasswordConfirmation);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                    if (user == null)
                    {
                        throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found");
                    }

                    var changePasswordResult = await _userManager.ChangePasswordAsync(user, setUserPasswordSpec.OldPassword, setUserPasswordSpec.NewPassword).ConfigureAwait(false);

                    if (!changePasswordResult.Succeeded)
                    {
                        HandleIdentityError(changePasswordResult.Errors);
                    }

                    await _signInManager.RefreshSignInAsync(user).ConfigureAwait(false);

                    await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                    transaction.Commit();
                }
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when setting password: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task SignOutUserAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await _signInManager.SignOutAsync().ConfigureAwait(false);

            _logger.LogInformation("User logged out");
        }

        public async Task<ApiResult<User>> GetUserAsync([FromRoute, Required] string userUuid, CancellationToken cancellationToken = default(CancellationToken))
        {
            userUuid = ProcessUserUuid(userUuid);

            try
            {
                var user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                if (user == null)
                {
                    throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found");
                }

                return new ApiResult<User>(new User
                {
                    Uuid = user.Id,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumber = user.PhoneNumber,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed
                });
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when getting user: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task<ApiResult<User>> UpdateUserAsync([FromRoute, Required] string userUuid, [FromBody] User user, CancellationToken cancellationToken = default(CancellationToken))
        {
            userUuid = ProcessUserUuid(userUuid);

            user.Email = ProcessEmail(user.Email, false);
            user.PhoneNumber = ProcessPhoneNumber(user.PhoneNumber, false);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var oldUser = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                    if (oldUser == null)
                    {
                        throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found");
                    }

                    bool changed = false;

                    if (user.PhoneNumberSet)
                    {
                        if (!string.Equals(oldUser.PhoneNumber, user.PhoneNumber))
                        {
                            var setPhoneNumberResult = await _userManager.SetPhoneNumberAsync(oldUser, user.PhoneNumber).ConfigureAwait(false);

                            if (!setPhoneNumberResult.Succeeded)
                            {
                                HandleIdentityError(setPhoneNumberResult.Errors);
                            }

                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        await _signInManager.RefreshSignInAsync(oldUser).ConfigureAwait(false);

                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();
                    }
                }

                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var newUser = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                    return new ApiResult<User>(new User
                    {
                        Uuid = newUser.Id,
                        Email = newUser.Email,
                        EmailConfirmed = newUser.EmailConfirmed,
                        PhoneNumber = newUser.PhoneNumber,
                        PhoneNumberConfirmed = newUser.PhoneNumberConfirmed
                    });
                }
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when updating user: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task ResendUserEmailConfirmationEmailAsync([FromRoute, Required] string userUuid, CancellationToken cancellationToken = default(CancellationToken))
        {
            userUuid = ProcessUserUuid(userUuid);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                    if (user == null)
                    {
                        throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found");
                    }

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);

                    var currentCultureInfo = Thread.CurrentThread.CurrentUICulture;

                    var callbackUrl = _urlHelper.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { userUuid = user.Id, code, culture = currentCultureInfo.ToString() },
                        protocol: "https");

                    EmailTemplate emailTemplate = await _emailTemplateProvider.GetConfirmAccountEmailAsync(
                        new ConfirmAccountEmailViewModel(callbackUrl), currentCultureInfo).ConfigureAwait(false);

                    await _emailSender.SendEmailAsync(user.Email, emailTemplate.Subject, emailTemplate.Body).ConfigureAwait(false);

                    await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                    transaction.Commit();
                }
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when resetting user password: {e}");

                throw new InternalServerErrorApiException();
            }
        }
    }
}
