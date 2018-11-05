﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HCore.Identity.Database.SqlServer;
using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Templating.Emails;
using HCore.Templating.Emails.ViewModels;
using HCore.Web.Exceptions;
using System.Collections.Generic;
using HCore.Identity.ViewModels;
using HCore.Web.API.Impl;
using IdentityServer4.Models;
using IdentityServer4;
using System.Linq;
using IdentityServer4.Validation;
using IdentityServer4.Services;
using IdentityServer4.Configuration;
using IdentityServer4.Stores;
using Microsoft.Extensions.Configuration;

namespace HCore.Identity.Controllers.API.Impl
{
    internal class IdentityServicesImpl : IIdentityServices
    {
        public const int MaxUserUuidLength = 50;
        public const int MaxUserNameLength = 50;
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 50;
        public const int MaxCodeLength = 512;

        private readonly SignInManager<UserModel> _signInManager;
        private readonly UserManager<UserModel> _userManager;
        private readonly ILogger<IdentityServicesImpl> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateProvider _emailTemplateProvider;
        private readonly IUrlHelper _urlHelper;
        private readonly SqlServerIdentityDbContext _identityDbContext;
        private readonly IUserClaimsPrincipalFactory<UserModel> _principalFactory;
        private readonly ITokenService _tokenService;
        private readonly IdentityServerOptions _options;
        private readonly IClientStore _clientStore;
        private readonly IResourceStore _resourceStore;
        private readonly IConfiguration _configuration;

        public IdentityServicesImpl(
            SignInManager<UserModel> signInManager,
            UserManager<UserModel> userManager,
            ILogger<IdentityServicesImpl> logger,
            IEmailSender emailSender,
            IEmailTemplateProvider emailTemplateProvider,
            IUrlHelper urlHelper,
            SqlServerIdentityDbContext identityDbContext,
            IUserClaimsPrincipalFactory<UserModel> principalFactory,
            ITokenService tokenService,
            IdentityServerOptions options,
            IClientStore clientStore,
            IResourceStore resourceStore,
            IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
            _emailTemplateProvider = emailTemplateProvider;
            _urlHelper = urlHelper;
            _identityDbContext = identityDbContext;
            _principalFactory = principalFactory;
            _tokenService = tokenService;
            _options = options;
            _clientStore = clientStore;
            _resourceStore = resourceStore;
            _configuration = configuration;
        }

        public async Task<UserModel> CreateUserAsync(UserSpec userSpec)
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

                        return user;
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

        public async Task<UserModel> SignInUserAsync(UserSignInSpec userSignInSpec)
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

                        return user;
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

        public async Task ConfirmUserEmailAddressAsync(string userUuid, UserConfirmEmailSpec userConfirmEmailSpec)
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

        public async Task UserForgotPasswordAsync(UserForgotPasswordSpec userForgotPasswordSpec)
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

        public async Task ResetUserPasswordAsync(ResetUserPasswordSpec resetUserPasswordSpec)
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

        public async Task SetUserPasswordAsync(string userUuid, SetUserPasswordSpec setUserPasswordSpec)
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

        public async Task SignOutUserAsync()
        {
            await _signInManager.SignOutAsync().ConfigureAwait(false);

            _logger.LogInformation("User logged out");
        }

        public async Task<UserModel> GetUserAsync(string userUuid)
        {
            userUuid = ProcessUserUuid(userUuid);

            try
            {
                var user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                if (user == null)
                {
                    throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found");
                }

                return user;
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

        public async Task<UserModel> UpdateUserAsync(string userUuid, UserSpec user)
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

                    return newUser;
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

        public async Task ResendUserEmailConfirmationEmailAsync(string userUuid)
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

        public async Task<string> GetAccessTokenAsync(string userUuid)
        {
            userUuid = ProcessUserUuid(userUuid);

            try
            {
                var tokenCreationRequest = new TokenCreationRequest();

                var user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                if (user == null)
                {
                    throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found");
                }
            
                var identityPricipal = await _principalFactory.CreateAsync(user).ConfigureAwait(false);

                var identityUser = new IdentityServerUser(user.Id.ToString());

                identityUser.AdditionalClaims = identityPricipal.Claims.ToArray();

                identityUser.DisplayName = user.UserName;

                identityUser.AuthenticationTime = DateTime.UtcNow;
                identityUser.IdentityProvider = IdentityServerConstants.LocalIdentityProvider;

                var subject = identityUser.CreatePrincipal();

                tokenCreationRequest.Subject = subject;
                tokenCreationRequest.IncludeAllIdentityClaims = true;

                tokenCreationRequest.ValidatedRequest = new ValidatedRequest();

                tokenCreationRequest.ValidatedRequest.Subject = tokenCreationRequest.Subject;

                string defaultClientId = _configuration[$"Identity:DefaultClient:ClientId"];
                if (string.IsNullOrEmpty(defaultClientId))
                    throw new Exception("Identity default client ID is empty");

                var client = await _clientStore.FindClientByIdAsync(defaultClientId).ConfigureAwait(false);

                tokenCreationRequest.ValidatedRequest.SetClient(client);

                var resources = await _resourceStore.GetAllEnabledResourcesAsync().ConfigureAwait(false);

                tokenCreationRequest.Resources = resources;

                tokenCreationRequest.ValidatedRequest.Options = _options;

                tokenCreationRequest.ValidatedRequest.ClientClaims = tokenCreationRequest.ValidatedRequest.ClientClaims.Concat(identityUser.AdditionalClaims).ToList();

                var accessToken = await _tokenService.CreateAccessTokenAsync(tokenCreationRequest).ConfigureAwait(false);

                string oidcAuthority = _configuration[$"Identity:DefaultClient:Authority"];
                if (string.IsNullOrEmpty(oidcAuthority))
                    throw new Exception("Identity OIDC authority string is empty");

                string oidcAudience = _configuration[$"Identity:DefaultClient:Audience"];
                if (string.IsNullOrEmpty(oidcAudience))
                    throw new Exception("Identity OIDC audience string is empty");

                accessToken.Issuer = oidcAuthority;
                accessToken.Audiences = new string[] { oidcAudience };

                var accessTokenValue = await _tokenService.CreateSecurityTokenAsync(accessToken);

                return accessTokenValue;
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when getting access token: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        private string ProcessUserUuid(string userUuid)
        {
            if (string.IsNullOrEmpty(userUuid))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidMissing, "The user UUID is missing");

            if (!ApiImpl.Uuid.IsMatch(userUuid))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidInvalid, "The user UUID is invalid");

            if (userUuid.Length > MaxUserUuidLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidInvalid, "The user UUID is invalid");

            return userUuid;
        }

        private string ProcessEmail(string email, bool required)
        {
            if (string.IsNullOrEmpty(email))
            {
                if (required)
                    throw new InvalidArgumentApiException(InvalidArgumentApiException.EmailMissing, "The email address is missing");

                return null;
            }

            if (!new EmailAddressAttribute().IsValid(email))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.EmailInvalid, "The email address is invalid");

            return email;
        }

        private string ProcessPhoneNumber(string phoneNumber, bool required)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                if (required)
                    throw new InvalidArgumentApiException(InvalidArgumentApiException.PhoneNumberMissing, "The phone number is missing");

                return null;
            }

            if (!new PhoneAttribute().IsValid(phoneNumber))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PhoneNumberInvalid, "The phone number is invalid");

            return phoneNumber;
        }

        private string ProcessPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordMissing, "The password is missing");
            
            if (password.Length < MinPasswordLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordTooShort, "The password is too short");

            if (password.Length > MaxPasswordLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordTooLong, "The password is too long");

            return password;
        }

        private string ProcessPasswordConfirmation(string password, string passwordConfirmation)
        {
            if (string.IsNullOrEmpty(passwordConfirmation))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordConfirmationMissing, "The password confirmation is missing");

            if (!Equals(password, passwordConfirmation))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordConfirmationNoMatch, "The password confirmation is not matching the password");

            return password;
        }

        private string ProcessCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.CodeMissing, "The code is missing");

            if (!ApiImpl.SafeString.IsMatch(code))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.CodeInvalid, "The code contains invalid characters");

            if (code.Length > MaxCodeLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.CodeTooLong, "The code is too long");

            return code;
        }

        private void HandleIdentityError(IEnumerable<IdentityError> errors)
        {
            var enumerator = errors.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var error = enumerator.Current;

                if (Equals(error.Code, "DuplicateUserName"))
                    throw new InvalidArgumentApiException(InvalidArgumentApiException.DuplicateUserName, "This user name already exists");
                else if (Equals(error.Code, "PasswordRequiresNonAlphanumeric"))
                    throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordRequiresNonAlphanumeric, "The password requires non alphanumeric characters");
                else if (Equals(error.Code, "InvalidToken"))
                    throw new InvalidArgumentApiException(InvalidArgumentApiException.SecurityTokenInvalid, "The security token is invalid or expired");
                else if (Equals(error.Code, "PasswordMismatch"))
                    throw new UnauthorizedApiException(UnauthorizedApiException.PasswordDoesNotMatch, "The password does not match our records");

                _logger.LogWarning($"Identity error was not covered: {error.Code}");
            }
            else
            {
                _logger.LogWarning("Unknown identity error occured");
            }

            throw new InternalServerErrorApiException();
        }
    }
}