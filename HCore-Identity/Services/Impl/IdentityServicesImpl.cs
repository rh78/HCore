using System;
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
using HCore.Identity.Models;
using HCore.Web.API.Impl;
using Microsoft.Extensions.DependencyInjection;
using HCore.Identity.Providers;
using HCore.Amqp.Messenger;
using HCore.Identity.Amqp;

namespace HCore.Identity.Services.Impl
{
    internal class IdentityServicesImpl : IIdentityServices
    {
        public const int MaxCodeLength = 512;

        private readonly SignInManager<UserModel> _signInManager;
        private readonly UserManager<UserModel> _userManager;
        private readonly ILogger<IdentityServicesImpl> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateProvider _emailTemplateProvider;
        private readonly IUrlHelper _urlHelper;
        private readonly SqlServerIdentityDbContext _identityDbContext;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IServiceProvider _serviceProvider;

        public IdentityServicesImpl(
            SignInManager<UserModel> signInManager,
            UserManager<UserModel> userManager,
            ILogger<IdentityServicesImpl> logger,
            IEmailSender emailSender,
            IEmailTemplateProvider emailTemplateProvider,
            IUrlHelper urlHelper,
            SqlServerIdentityDbContext identityDbContext,
            IConfigurationProvider configurationProvider,
            IServiceProvider serviceProvider)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
            _emailTemplateProvider = emailTemplateProvider;
            _urlHelper = urlHelper;
            _identityDbContext = identityDbContext;
            _configurationProvider = configurationProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<UserModel> CreateUserAsync(UserSpec userSpec, bool isAdmin)
        {
            if (!isAdmin)
            {
                if (!_configurationProvider.SelfRegistration)
                    throw new ForbiddenApiException(ForbiddenApiException.SelfRegistrationNotAllowed, "It is not allowed to register users in self-service on this system");
            }

            userSpec.Email = ProcessEmail(userSpec.Email);
            userSpec.Password = ProcessPassword(userSpec.Password);

            if (_configurationProvider.SelfManagement)
            {
                if (_configurationProvider.ManageName && _configurationProvider.RegisterName)
                {
                    userSpec.FirstName = ProcessFirstName(userSpec.FirstName);
                    userSpec.LastName = ProcessLastName(userSpec.LastName);
                }

                if (_configurationProvider.ManagePhoneNumber && _configurationProvider.RegisterPhoneNumber)
                {
                    userSpec.PhoneNumber = ProcessPhoneNumber(userSpec.PhoneNumber);
                }
            } 

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = new UserModel { UserName = userSpec.Email, Email = userSpec.Email };

                    if (_configurationProvider.SelfManagement)
                    {
                        if (_configurationProvider.ManageName && _configurationProvider.RegisterName)
                        {
                            user.FirstName = userSpec.FirstName;
                            user.LastName = userSpec.LastName;
                        }

                        if (_configurationProvider.ManagePhoneNumber && _configurationProvider.RegisterPhoneNumber)
                        {
                            user.PhoneNumber = userSpec.PhoneNumber;
                        }
                    }

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

                        await SendUserChangeNotificationAsync(user.Id).ConfigureAwait(false);

                        if (!_configurationProvider.RequireEmailConfirmed || user.EmailConfirmed)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false).ConfigureAwait(false);
                        }

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
            userSignInSpec.Email = ProcessEmail(userSignInSpec.Email);
            userSignInSpec.Password = ProcessPassword(userSignInSpec.Password);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = await _userManager.FindByEmailAsync(userSignInSpec.Email).ConfigureAwait(false);

                    if (user == null)
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.InvalidCredentials, "The user credentials are not valid");
                    }

                    bool remember = userSignInSpec.RememberSet && userSignInSpec.Remember;

                    var result = await _signInManager.PasswordSignInAsync(userSignInSpec.Email, userSignInSpec.Password, remember, lockoutOnFailure: true).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User signed in");

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
                    else if (result.IsNotAllowed)
                    {
                        var exception = new UnauthorizedApiException(UnauthorizedApiException.EmailNotConfirmed, "The email address is not yet confirmed");

                        exception.SetUserUuid(user.Id);

                        throw exception;
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

                        await SendUserChangeNotificationAsync(user.Id).ConfigureAwait(false);
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
            userForgotPasswordSpec.Email = ProcessEmail(userForgotPasswordSpec.Email);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = await _userManager.FindByEmailAsync(userForgotPasswordSpec.Email).ConfigureAwait(false);

                    if (user == null)
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
            resetUserPasswordSpec.Email = ProcessEmail(resetUserPasswordSpec.Email);
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

        public async Task<UserModel> UpdateUserAsync(string userUuid, UserSpec userSpec, bool isAdmin)
        {
            if (!_configurationProvider.ManageName && !_configurationProvider.ManagePhoneNumber)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserDetailsNotSupported, "This system does not support managing user detail data");

            if (!isAdmin) {
                if (!_configurationProvider.SelfManagement)
                    throw new ForbiddenApiException(ForbiddenApiException.SelfServiceNotAllowed, "It is not allowed to modify user data in self-service on this system");
            }
            
            userUuid = ProcessUserUuid(userUuid);
            
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

                    if (_configurationProvider.ManageName)
                    {
                        if (!string.IsNullOrEmpty(userSpec.FirstName))
                        {
                            userSpec.FirstName = ProcessFirstName(userSpec.FirstName);

                            if (!string.Equals(oldUser.FirstName, userSpec.FirstName))
                            {
                                oldUser.FirstName = userSpec.FirstName;

                                changed = true;
                            }
                        }

                        if (!string.IsNullOrEmpty(userSpec.LastName))
                        {
                            userSpec.LastName = ProcessLastName(userSpec.LastName);

                            if (!string.Equals(oldUser.LastName, userSpec.LastName))
                            {
                                oldUser.LastName = userSpec.LastName;

                                changed = true;
                            }
                        }
                    }

                    if (_configurationProvider.ManagePhoneNumber)
                    {
                        if (!string.IsNullOrEmpty(userSpec.PhoneNumber))
                        {
                            userSpec.PhoneNumber = ProcessPhoneNumber(userSpec.PhoneNumber);

                            if (!string.Equals(oldUser.PhoneNumber, userSpec.PhoneNumber))
                            {
                                oldUser.PhoneNumber = userSpec.PhoneNumber;

                                changed = true;
                            }
                        }
                    }

                    if (changed)
                    {
                        var updateResult = await _userManager.UpdateAsync(oldUser);

                        if (!updateResult.Succeeded)
                        {
                            HandleIdentityError(updateResult.Errors);
                        }

                        await _signInManager.RefreshSignInAsync(oldUser).ConfigureAwait(false);

                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();

                        await SendUserChangeNotificationAsync(oldUser.Id).ConfigureAwait(false);                        
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

        private string ProcessUserUuid(string userUuid)
        {
            if (string.IsNullOrEmpty(userUuid))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidMissing, "The user UUID is missing");

            if (!ApiImpl.Uuid.IsMatch(userUuid))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidInvalid, "The user UUID is invalid");

            if (userUuid.Length > UserModel.MaxUserUuidLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidInvalid, "The user UUID is invalid");

            return userUuid;
        }

        private string ProcessEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.EmailMissing, "The email address is missing");
            
            if (!new EmailAddressAttribute().IsValid(email))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.EmailInvalid, "The email address is invalid");

            if (email.Length > UserModel.MaxEmailAddressLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.EmailInvalid, "The email address is too long");

            return email;
        }

        private string ProcessFirstName(string firstName)
        {
            if (string.IsNullOrEmpty(firstName))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.FirstNameMissing, "The first name is missing");
            
            if (!ApiImpl.SafeString.IsMatch(firstName))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.FirstNameInvalid, "The first name contains invalid characters");

            if (firstName.Length > UserModel.MaxFirstNameLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.FirstNameTooLong, "The fist name is too long");

            return firstName;
        }

        private string ProcessLastName(string lastName)
        {
            if (string.IsNullOrEmpty(lastName))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.LastNameMissing, "The last name is missing");
            
            if (!ApiImpl.SafeString.IsMatch(lastName))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.LastNameInvalid, "The last name contains invalid characters");

            if (lastName.Length > UserModel.MaxLastNameLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.LastNameTooLong, "The last name is too long");

            return lastName;
        }

        private string ProcessPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PhoneNumberMissing, "The phone number is missing");
            
            if (!new PhoneAttribute().IsValid(phoneNumber))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PhoneNumberInvalid, "The phone number is invalid");

            return phoneNumber;
        }

        private string ProcessPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordMissing, "The password is missing");
            
            if (password.Length < UserModel.MinPasswordLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordTooShort, "The password is too short");

            if (password.Length > UserModel.MaxPasswordLength)
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

        private async Task SendUserChangeNotificationAsync(string userUuid)
        {
            if (!string.IsNullOrEmpty(_configurationProvider.IdentityChangeTasksAmqpAddress))
            {
                // notification to dependent services

                string identityChangeTasksAmqpAddress = _configurationProvider.IdentityChangeTasksAmqpAddress;

                var identityChangeTask = new IdentityChangeTask()
                {
                    UserUuid = userUuid
                };

                var amqpMessenger = _serviceProvider.GetRequiredService<IAMQPMessenger>();

                await amqpMessenger.SendMessageAsync(identityChangeTasksAmqpAddress, identityChangeTask).ConfigureAwait(false);
            }
        }
    }
}
