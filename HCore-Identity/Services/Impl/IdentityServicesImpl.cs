using System;
using System.ComponentModel.DataAnnotations;
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
using HCore.Amqp.Messenger;
using HCore.Identity.Amqp;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using HCore.Web.Providers;
using HCore.Tenants.Providers;
using Microsoft.AspNetCore.Authentication;
using IdentityModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using HCore.Tenants;
using HCore.Identity.Internal;
using reCAPTCHA.AspNetCore;
using HCore.Tenants.Models;
using Microsoft.Extensions.Configuration;
using Kickbox;
using System.Web;
using HCore.Identity.Listeners;

namespace HCore.Identity.Services.Impl
{
    public class IdentityServicesImpl : IIdentityServices
    {
        public const int MaxCodeLength = 512;

        public const string AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+:";

        private readonly SignInManager<UserModel> _signInManager;
        private readonly UserManager<UserModel> _userManager;
        private readonly ILogger<IdentityServicesImpl> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateProvider _emailTemplateProvider;
        private readonly IUrlHelper _urlHelper;
        private readonly SqlServerIdentityDbContext _identityDbContext;
        private readonly Providers.IConfigurationProvider _configurationProvider;       

        private readonly INowProvider _nowProvider;

        private readonly IUserNotificationListener _userNotificationListener;

        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        private readonly IRecaptchaService _recaptchaService;

        private readonly string _emailValidationApiKey;

        private readonly IServiceProvider _serviceProvider;

        private readonly List<string> _devAdminSsoProtectedUserAccountEmailAddresses = new List<string>();

        private readonly List<string> _devAdminSsoAuthorizedIssuers = new List<string>();

        private readonly bool _blockLowQuality = false;

        public IdentityServicesImpl(
            SignInManager<UserModel> signInManager,
            UserManager<UserModel> userManager,
            ILogger<IdentityServicesImpl> logger,
            IEmailSender emailSender,
            IEmailTemplateProvider emailTemplateProvider,
            IUrlHelper urlHelper,
            SqlServerIdentityDbContext identityDbContext,
            Providers.IConfigurationProvider configurationProvider,
            INowProvider nowProvider,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
            _emailTemplateProvider = emailTemplateProvider;
            _urlHelper = urlHelper;
            _identityDbContext = identityDbContext;
            _configurationProvider = configurationProvider;

            _nowProvider = nowProvider;

            _serviceProvider = serviceProvider;

            _userNotificationListener = serviceProvider.GetService<IUserNotificationListener>();

            _tenantInfoAccessor = serviceProvider.GetService<ITenantInfoAccessor>();

            _recaptchaService = serviceProvider.GetService<IRecaptchaService>();

            var devAdminSsoProtectedUserAccountEmailAddresses = new List<string>();

            configuration.GetSection("Identity:Tenants:DevAdminSsoProtectedUserAccountEmailAddresses")?.Bind(devAdminSsoProtectedUserAccountEmailAddresses);

            devAdminSsoProtectedUserAccountEmailAddresses.ForEach((devAdminSsoProtectedUserAccountEmailAddress) =>
            {
                if (!string.IsNullOrEmpty(devAdminSsoProtectedUserAccountEmailAddress))
                {
                    _devAdminSsoProtectedUserAccountEmailAddresses.Add(devAdminSsoProtectedUserAccountEmailAddress.Trim().ToUpperInvariant());
                }
            });

            var devAdminSsoAuthorizedIssuers = new List<string>();

            configuration.GetSection("Identity:Tenants:DevAdminSsoAuthorizedIssuers")?.Bind(devAdminSsoAuthorizedIssuers);

            devAdminSsoAuthorizedIssuers.ForEach((devAdminSsoAuthorizedIssuer) =>
            {
                if (!string.IsNullOrEmpty(devAdminSsoAuthorizedIssuer))
                {
                    _devAdminSsoAuthorizedIssuers.Add(devAdminSsoAuthorizedIssuer);
                }
            });

            _emailValidationApiKey = configuration["Identity:EmailValidation:Kickbox:ApiKey"];

            if (string.IsNullOrEmpty(_emailValidationApiKey))
            {
                _emailValidationApiKey = null;
            }

            _blockLowQuality = configuration.GetValue<bool?>("Identity:EmailValidation:Kickbox:BlockLowQuality") ?? false;
        }

        public async Task<ReservedEmailAddressModel> ReserveUserUuidAsync(long? developerUuid, long? tenantUuid, string unscopedEmailAddress, bool processEmailAddress = true, bool createReservationIfNotPresent = true)
        {
            if (processEmailAddress)
                unscopedEmailAddress = ProcessEmail(unscopedEmailAddress);

            ITenantInfo tenantInfo = null;

            if (_tenantInfoAccessor != null)
                tenantInfo = _tenantInfoAccessor.TenantInfo;

            var scopedEmailAddress = GetScopedEmail(developerUuid, tenantUuid, unscopedEmailAddress);

            string normalizedScopedEmailAddress = Normalize(scopedEmailAddress);

            try
            {
                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    // make sure we have no race conditions

                    if (_identityDbContext.Database.ProviderName != null && _identityDbContext.Database.ProviderName.StartsWith("Npgsql"))
                    {
                        await _identityDbContext.Database.ExecuteSqlRawAsync("SELECT \"Uuid\" FROM public.\"ReservedEmailAddresses\" WHERE \"NormalizedEmailAddress\" = {0} FOR UPDATE", normalizedScopedEmailAddress).ConfigureAwait(false);
                    }
                    else
                    {
                        await _identityDbContext.Database.ExecuteSqlRawAsync("SELECT Uuid FROM ReservedEmailAddresses WITH (ROWLOCK, XLOCK, HOLDLOCK) WHERE NormalizedEmailAddress = {0}", normalizedScopedEmailAddress).ConfigureAwait(false);
                    }

                    IQueryable<ReservedEmailAddressModel> query = _identityDbContext.ReservedEmailAddresses;

                    query = query.Where(reservedEmailAddressQuery => reservedEmailAddressQuery.NormalizedEmailAddress == normalizedScopedEmailAddress);

                    var reservedEmailAddressModel = await query.FirstOrDefaultAsync().ConfigureAwait(false);

                    if (reservedEmailAddressModel != null)
                        return reservedEmailAddressModel;

                    if (!createReservationIfNotPresent)
                        return null;

                    string newScopedUserUuid = GetNewScopedUuid(developerUuid, tenantUuid);

                    reservedEmailAddressModel = new ReservedEmailAddressModel()
                    {
                        Uuid = newScopedUserUuid,
                        NormalizedEmailAddress = normalizedScopedEmailAddress
                    };

                    _identityDbContext.ReservedEmailAddresses.Add(reservedEmailAddressModel);

                    await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                    transaction.Commit();

                    if (_userNotificationListener != null)
                    {
                        await _userNotificationListener.UserReservedUuidAsync(newScopedUserUuid, normalizedScopedEmailAddress).ConfigureAwait(false);
                    }

                    // no expiry date or disabled/deleted upon reservation supported right now

                    return reservedEmailAddressModel;
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when reserving user UUID: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task<UserModel> CreateUserAsync(UserSpec userSpec, bool isSelfRegistration, bool emailIsAlreadyConfirmed = false, HttpRequest request = null, bool requiresRecaptcha = true, RecaptchaSettings recaptchaSettings = null, bool ignoreSelfRegistrationRestriction = false)
        {
            if (isSelfRegistration)
            {
                if (!_configurationProvider.SelfRegistration && !ignoreSelfRegistrationRestriction)
                    throw new ForbiddenApiException(ForbiddenApiException.SelfRegistrationNotAllowed, "It is not allowed to register users in self-service on this system");
            }

            userSpec.Email = ProcessEmail(userSpec.Email);

            if (!string.IsNullOrEmpty(_emailValidationApiKey))
            {
                var kickBoxApi = new KickBoxApi(_emailValidationApiKey);

                var response = await kickBoxApi.VerifyWithResponse(HttpUtility.UrlEncode(userSpec.Email)).ConfigureAwait(false);

                if (response.Success)
                {
                    switch (response.Reason) {
                        case "invalid_email":
                        case "invalid_domain":
                            {
                                _logger.LogWarning($"Discovered invalid email address: {userSpec.Email}, reason: {response.Reason}, {response.Message}");

                                throw new RequestFailedApiException(RequestFailedApiException.EmailInvalid, "The email address is invalid");
                            }
                        case "rejected_email":
                            {
                                _logger.LogWarning($"Discovered rejected email address: {userSpec.Email}, reason: {response.Reason}, {response.Message}");

                                throw new RequestFailedApiException(RequestFailedApiException.EmailNotExisting, "This e-mail address does not exist");
                            }
                        case "low_quality":
                            {
                                if (_blockLowQuality)
                                {
                                    _logger.LogWarning($"Discovered low quality email address: {userSpec.Email}, reason: {response.Reason}, {response.Message}");

                                    throw new RequestFailedApiException(RequestFailedApiException.EmailRequiresBusinessAccount, "Please use your business email account to register for our service");
                                }
                            }

                            break;
                        default:
                            // low_quality - ignore for now
                            // low_deliverability - ignore for now
                            // no_connect - ignore for now
                            // timeout - ignore for now
                            // invalid_smtp - ignore for now
                            // unavailable_smtp - ignore for now
                            // unexpected_error - ignore for now

                            break;
                    }

                    // role - ignore for now
                    // free - ignore for now
                    // accept_all - ignore for now

                    if (response.Disposable)
                    {
                        _logger.LogWarning($"Discovered disposable email address: {userSpec.Email}, reason: {response.Reason}, {response.Message}");

                        throw new RequestFailedApiException(RequestFailedApiException.NoDisposableEmailsAllowed, "Please do not use an disposable e-mail address");
                    }
                    
                    if (response.Free && _blockLowQuality)
                    {
                        _logger.LogWarning($"Discovered free email address: {userSpec.Email}, reason: {response.Reason}, {response.Message}");

                        throw new RequestFailedApiException(RequestFailedApiException.EmailRequiresBusinessAccount, "Please use your business email account to register for our service");
                    }
                }
            }

            HandleEnforceExternalAuthenticationForEmailDomains(userSpec.Email);

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

            userSpec.Password = ProcessPassword(userSpec.Password);
            userSpec.PasswordConfirmation = ProcessPasswordConfirmation(userSpec.Password, userSpec.PasswordConfirmation);

            bool requiresTermsAndConditions = _configurationProvider.RequiresTermsAndConditions;

            if (_tenantInfoAccessor != null)
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                if (tenantInfo != null)
                    requiresTermsAndConditions = tenantInfo.RequiresTermsAndConditions;
            }

            if (requiresTermsAndConditions)
            {
                if (!userSpec.AcceptTermsAndConditions && !userSpec.AcceptTermsAndConditionsAndPrivacyPolicy)
                    throw new RequestFailedApiException(RequestFailedApiException.PleaseAcceptTermsAndConditions, "Please accept the terms and conditions");
            }

            if (!userSpec.AcceptPrivacyPolicy && !userSpec.AcceptTermsAndConditionsAndPrivacyPolicy)
                throw new RequestFailedApiException(RequestFailedApiException.PleaseAcceptPrivacyPolicy, "Please accept the privacy policy");

            if (requiresRecaptcha)
            {
                if (_recaptchaService != null && request != null)
                {
                    var recaptchaValidationResult = await _recaptchaService.Validate(request);

                    if (!recaptchaValidationResult.success)
                        throw new RequestFailedApiException(RequestFailedApiException.RecaptchaValidationFailed, "The reCAPTCHA validation failed");
                }
            }

            try
            {
                var reservedEmailAddressModel = await ReserveUserUuidAsync(developerUuid: null, tenantUuid: null, userSpec.Email, createReservationIfNotPresent: true).ConfigureAwait(false);

                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var existingUser = await _userManager.FindByEmailAsync(userSpec.Email).ConfigureAwait(false);

                    if (existingUser != null)
                        throw new RequestFailedApiException(RequestFailedApiException.EmailAlreadyExists, "A user account with that email address already exists");

                    var userDeletedModel = await GetUserDeletedAsync(reservedEmailAddressModel.Uuid).ConfigureAwait(false);

                    if (userDeletedModel != null)
                    {
                        throw new RequestFailedApiException(RequestFailedApiException.EmailAlreadyExists, "It is not possible to reuse a user ID that belongs to a deleted user");
                    }

                    var user = new UserModel { 
                        Id = reservedEmailAddressModel.Uuid, 
                        UserName = userSpec.Email, 
                        Email = userSpec.Email, 
                        NormalizedEmailWithoutScope = Normalize(userSpec.Email),
                        ExpiryDate = reservedEmailAddressModel.ExpiryDate,
                        Disabled = reservedEmailAddressModel.Disabled
                    };

                    if (user.Disabled == true)
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.AccountDisabled, "The user account is disabled");
                    }

                    if (user.ExpiryDate != null && user.ExpiryDate < DateTimeOffset.Now)
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.AccountExpired, "The user account is expired");
                    }

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

                    if (emailIsAlreadyConfirmed)
                    {
                        user.EmailConfirmed = true;
                    }

                    user.NotificationCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                    user.GroupNotifications = false;
                    user.Currency = GetDefaultCurrency();

                    user.PrivacyPolicyAccepted = _nowProvider.Now;
                    user.PrivacyPolicyUrl = _configurationProvider.PrivacyPolicyUrl;
                    user.PrivacyPolicyVersionAccepted = _configurationProvider.PrivacyPolicyVersion;

                    if (userSpec.AcceptCommunication)
                    {
                        user.CommunicationAccepted = _nowProvider.Now;
                        user.CommunicationUrl = _configurationProvider.PrivacyPolicyUrl;
                        user.CommunicationVersionAccepted = _configurationProvider.PrivacyPolicyVersion;
                    }

                    if (requiresTermsAndConditions)
                    {
                        user.TermsAndConditionsAccepted = _nowProvider.Now;
                        user.TermsAndConditionsUrl = _configurationProvider.TermsAndConditionsUrl;
                        user.TermsAndConditionsVersionAccepted = _configurationProvider.TermsAndConditionsVersion;
                    }

                    if (_tenantInfoAccessor != null)
                    {
                        var tenantInfo = _tenantInfoAccessor.TenantInfo;

                        if (tenantInfo != null)
                        {
                            string tenantPrivacyPolicyUrl = tenantInfo.DeveloperPrivacyPolicyUrl;
                            if (!string.IsNullOrEmpty(tenantPrivacyPolicyUrl))
                            {
                                user.PrivacyPolicyUrl = tenantPrivacyPolicyUrl;

                                if (userSpec.AcceptCommunication)
                                    user.CommunicationUrl = tenantPrivacyPolicyUrl;
                            }

                            int? tenantPrivacyPolicyVersion = tenantInfo.DeveloperPrivacyPolicyVersion;
                            if (tenantPrivacyPolicyVersion != null && tenantPrivacyPolicyVersion > 0)
                            {
                                user.PrivacyPolicyVersionAccepted = tenantPrivacyPolicyVersion;

                                if (userSpec.AcceptCommunication)
                                    user.CommunicationVersionAccepted = tenantPrivacyPolicyVersion;
                            }

                            if (requiresTermsAndConditions)
                            {
                                string tenantTermsAndConditionsUrl = tenantInfo.DeveloperTermsAndConditionsUrl;
                                if (!string.IsNullOrEmpty(tenantTermsAndConditionsUrl))
                                    user.TermsAndConditionsUrl = tenantTermsAndConditionsUrl;

                                int? tenantTermsAndConditionsVersion = tenantInfo.DeveloperTermsAndConditionsVersion;
                                if (tenantTermsAndConditionsVersion != null && tenantTermsAndConditionsVersion > 0)
                                    user.TermsAndConditionsVersionAccepted = tenantTermsAndConditionsVersion;
                            }
                        }
                    }

                    var result = await _userManager.CreateAsync(user, userSpec.Password).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password");

                        if (!emailIsAlreadyConfirmed)
                        {
                            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);

                            var currentCultureInfo = CultureInfo.CurrentCulture;

                            var callbackUrl = _urlHelper.Page(
                                "/Account/ConfirmEmail",
                                pageHandler: null,
                                values: new { userUuid = user.Id, code, culture = currentCultureInfo.ToString() },
                                protocol: "https");

                            EmailTemplate emailTemplate = await _emailTemplateProvider.GetConfirmAccountEmailAsync(
                                new ConfirmAccountEmailViewModel(callbackUrl), isPortals: null, currentCultureInfo).ConfigureAwait(false);

                            await _emailSender.SendEmailAsync(userSpec.Email, emailTemplate.Subject, emailTemplate.Body).ConfigureAwait(false);
                        }

                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();

                        if (_userNotificationListener != null)
                        {
                            var userNotificationModel = new UserNotificationModel
                            {
                                Id = user.Id,
                                Email = user.Email,
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                PhoneNumber = user.PhoneNumber,
                                NotificationCulture = user.NotificationCulture,
                                GroupNotifications = user.GroupNotifications,
                                Currency = user.Currency,
                                ExternalUuid = user.ExternalUuid
                            };

                            await _userNotificationListener.UserCreatedAsync(userNotificationModel).ConfigureAwait(false);
                        }

                        await SendUserChangeNotificationAsync(user.Id).ConfigureAwait(false);

                        if (isSelfRegistration)
                        {
                            if (!_configurationProvider.RequireEmailConfirmed || user.EmailConfirmed)
                            {
                                await _signInManager.SignInAsync(user, isPersistent: false).ConfigureAwait(false);

                                if (_userNotificationListener != null)
                                {
                                    await _userNotificationListener.UserLoggedInAsync(user.Id).ConfigureAwait(false);
                                }
                            }
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
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when registering user: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        // create through external authentication provider

        private async Task<UserModel> CreateUserAsync(long developerUuid, long tenantUuid, AuthenticationTicket authenticationTicket, ClaimsPrincipal externalUser, string userIdClaimValue, long? targetTenantUuid)
        {
            string unscopedEmailAddress = ProcessEmail(externalUser);

            string firstName = null;
            string lastName = null;
            string phoneNumber = null;

            if (_configurationProvider.ManageName && _configurationProvider.RegisterName)
            {
                firstName = ProcessFirstName(externalUser);
                lastName = ProcessLastName(externalUser);
            }

            if (_configurationProvider.ManagePhoneNumber && _configurationProvider.RegisterPhoneNumber)
            {
                phoneNumber = ProcessPhoneNumber(externalUser);
            }

            HashSet<string> memberOf = ProcessMemberOf(externalUser);

            /* bool requiresTermsAndConditions = _configurationProvider.RequiresTermsAndConditions;

            if (_tenantInfoAccessor != null)
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                if (tenantInfo != null)
                    requiresTermsAndConditions = tenantInfo.RequiresTermsAndConditions;
            }

            if (requiresTermsAndConditions)
            {
                if (!userSpec.AcceptTermsAndConditions && !userSpec.AcceptTermsAndConditionsAndPrivacyPolicy)
                    throw new RequestFailedApiException(RequestFailedApiException.PleaseAcceptTermsAndConditions, "Please accept the terms and conditions");
            }

            if (!userSpec.AcceptPrivacyPolicy && !userSpec.AcceptTermsAndConditionsAndPrivacyPolicy)
                throw new RequestFailedApiException(RequestFailedApiException.PleaseAcceptPrivacyPolicy, "Please accept the privacy policy"); */

            string normalizedUnscopedEmailAddress = Normalize(unscopedEmailAddress);

            if (_devAdminSsoProtectedUserAccountEmailAddresses.Contains(normalizedUnscopedEmailAddress))
            {
                string issuer = ProcessIssuer(externalUser);

                if (!_devAdminSsoAuthorizedIssuers.Contains(issuer))
                    throw new Exception("The external authentication failed");
            }

            ITenantInfo tenantInfo = null;

            if (_tenantInfoAccessor != null)
                tenantInfo = _tenantInfoAccessor.TenantInfo;

            var externalAuthenticationAllowUserMerge = tenantInfo?.ExternalAuthenticationAllowUserMerge ?? false;

            string scopedEmailAddress;

            if (externalAuthenticationAllowUserMerge)
            {
                scopedEmailAddress = unscopedEmailAddress;
            }
            else
            {
                scopedEmailAddress = GetScopedEmail(developerUuid, tenantUuid, unscopedEmailAddress);
            }

            try
            {
                ReservedEmailAddressModel reservedEmailAddressModel;

                if (externalAuthenticationAllowUserMerge)
                {
                    reservedEmailAddressModel = await ReserveUserUuidAsync(developerUuid: null, tenantUuid: null, unscopedEmailAddress, createReservationIfNotPresent: true).ConfigureAwait(false);
                }
                else
                {
                    reservedEmailAddressModel = await ReserveUserUuidAsync(developerUuid, tenantUuid, unscopedEmailAddress, createReservationIfNotPresent: true).ConfigureAwait(false);
                }

                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var existingUser = await _userManager.FindByEmailAsync(scopedEmailAddress).ConfigureAwait(false);

                    if (existingUser != null)
                        throw new RequestFailedApiException(RequestFailedApiException.EmailAlreadyExists, "A user account with that email address already exists");

                    var userDeletedModel = await GetUserDeletedAsync(reservedEmailAddressModel.Uuid).ConfigureAwait(false);

                    if (userDeletedModel != null)
                    {
                        throw new RequestFailedApiException(RequestFailedApiException.EmailAlreadyExists, "It is not possible to reuse a user ID that belongs to a deleted user");
                    }

                    UserModel user;

                    if (externalAuthenticationAllowUserMerge)
                    {
                        user = new UserModel
                        {
                            Id = reservedEmailAddressModel.Uuid,
                            UserName = scopedEmailAddress,
                            Email = scopedEmailAddress,
                            MemberOf = memberOf?.ToList(),
                            NormalizedEmailWithoutScope = normalizedUnscopedEmailAddress,
                            ExpiryDate = reservedEmailAddressModel.ExpiryDate,
                            Disabled = reservedEmailAddressModel.Disabled
                        };
                    }
                    else
                    {
                        user = new UserModel
                        {
                            Id = reservedEmailAddressModel.Uuid,
                            UserName = reservedEmailAddressModel.Uuid,
                            Email = scopedEmailAddress,
                            MemberOf = memberOf?.ToList(),
                            NormalizedEmailWithoutScope = normalizedUnscopedEmailAddress,
                            ExpiryDate = reservedEmailAddressModel.ExpiryDate,
                            Disabled = reservedEmailAddressModel.Disabled
                        };
                    }

                    if (user.Disabled == true)
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.AccountDisabled, "The user account is disabled");
                    }

                    if (user.ExpiryDate != null && user.ExpiryDate < DateTimeOffset.Now)
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.AccountExpired, "The user account is expired");
                    }

                    if (!string.IsNullOrEmpty(userIdClaimValue))
                    {
                        user.ExternalUuid = userIdClaimValue;
                    }

                    if (_configurationProvider.ManageName && _configurationProvider.RegisterName)
                    {
                        user.FirstName = firstName;
                        user.LastName = lastName;
                    }

                    if (_configurationProvider.ManagePhoneNumber && _configurationProvider.RegisterPhoneNumber)
                    {
                        user.PhoneNumber = phoneNumber;
                    }

                    var identityToken = ProcessIdentityToken(authenticationTicket);
                    user.IdentityTokenCache = identityToken;

                    var accessToken = ProcessAccessToken(authenticationTicket);
                    var refreshToken = ProcessRefreshToken(authenticationTicket);

                    // we'll have external emails always confirmed

                    user.EmailConfirmed = true;

                    user.NotificationCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                    user.GroupNotifications = false;
                    user.Currency = GetDefaultCurrency();

                    user.PrivacyPolicyAccepted = _nowProvider.Now;
                    user.PrivacyPolicyUrl = _configurationProvider.PrivacyPolicyUrl;
                    user.PrivacyPolicyVersionAccepted = _configurationProvider.PrivacyPolicyVersion;

                    /* if (userSpec.AcceptCommunication)
                    {
                        user.CommunicationAccepted = _nowProvider.Now;
                        user.CommunicationUrl = _configurationProvider.PrivacyPolicyUrl;
                        user.CommunicationVersionAccepted = _configurationProvider.PrivacyPolicyVersion;
                    }

                    if (requiresTermsAndConditions)
                    {
                        user.TermsAndConditionsAccepted = _nowProvider.Now;
                        user.TermsAndConditionsUrl = _configurationProvider.TermsAndConditionsUrl;
                        user.TermsAndConditionsVersionAccepted = _configurationProvider.TermsAndConditionsVersion;
                    } */

                    if (tenantInfo != null)
                    {
                        string tenantPrivacyPolicyUrl = tenantInfo.DeveloperPrivacyPolicyUrl;
                        if (!string.IsNullOrEmpty(tenantPrivacyPolicyUrl))
                        {
                            user.PrivacyPolicyUrl = tenantPrivacyPolicyUrl;

                            /* if (userSpec.AcceptCommunication)
                                user.CommunicationUrl = tenantPrivacyPolicyUrl; */
                        }

                        int? tenantPrivacyPolicyVersion = tenantInfo.DeveloperPrivacyPolicyVersion;
                        if (tenantPrivacyPolicyVersion != null && tenantPrivacyPolicyVersion > 0)
                        {
                            user.PrivacyPolicyVersionAccepted = tenantPrivacyPolicyVersion;

                            /* if (userSpec.AcceptCommunication)
                                user.CommunicationVersionAccepted = tenantPrivacyPolicyVersion; */
                        }

                        /* if (requiresTermsAndConditions)
                        {
                            string tenantTermsAndConditionsUrl = tenantInfo.DeveloperTermsAndConditionsUrl;
                            if (!string.IsNullOrEmpty(tenantTermsAndConditionsUrl))
                                user.TermsAndConditionsUrl = tenantTermsAndConditionsUrl;

                            int? tenantTermsAndConditionsVersion = tenantInfo.DeveloperTermsAndConditionsVersion;
                            if (tenantTermsAndConditionsVersion != null && tenantTermsAndConditionsVersion > 0)
                                user.TermsAndConditionsVersionAccepted = tenantTermsAndConditionsVersion;
                        } */
                    }

                    var result = await _userManager.CreateAsync(user).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("External user created without password");

                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();

                        if (_userNotificationListener != null)
                        {
                            var userNotificationModel = new UserNotificationModel
                            {
                                Id = user.Id,
                                Email = user.Email,
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                PhoneNumber = user.PhoneNumber,
                                NotificationCulture = user.NotificationCulture,
                                GroupNotifications = user.GroupNotifications,
                                Currency = user.Currency,
                                ExternalUuid = user.ExternalUuid
                            };

                            await _userNotificationListener.UserCreatedAsync(userNotificationModel).ConfigureAwait(false);
                        }

                        await SendUserChangeNotificationAsync(user.Id, accessToken, refreshToken, targetTenantUuid, trySynchronousFirst: true).ConfigureAwait(false);

                        if (!_configurationProvider.RequireEmailConfirmed || user.EmailConfirmed)
                        {
                            _signInManager.ClaimsFactory = new Saml2SupportClaimsFactory(_signInManager.ClaimsFactory, externalUser);

                            await _signInManager.SignInAsync(user, isPersistent: false).ConfigureAwait(false);

                            if (_userNotificationListener != null)
                            {
                                await _userNotificationListener.UserLoggedInAsync(user.Id).ConfigureAwait(false);
                            }
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
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when registering external user: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task<UserModel> SignInUserAsync(UserSignInSpec userSignInSpec)
        {
            userSignInSpec.Email = ProcessEmail(userSignInSpec.Email);
            userSignInSpec.Password = ProcessPassword(userSignInSpec.Password);

            try
            {
                HandleEnforceExternalAuthenticationForEmailDomains(userSignInSpec.Email);

                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = await _userManager.FindByEmailAsync(userSignInSpec.Email).ConfigureAwait(false);

                    if (user == null)
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.InvalidCredentials, "The user credentials are not valid");
                    }

                    Microsoft.AspNetCore.Identity.SignInResult result;

                    if (user.Disabled == true)
                    {
                        result = await _signInManager.CheckPasswordSignInAsync(user, userSignInSpec.Password, lockoutOnFailure: true).ConfigureAwait(false);

                        if (result.Succeeded)
                        {
                            throw new UnauthorizedApiException(UnauthorizedApiException.AccountDisabled, "The user account is disabled");
                        }
                    }

                    if (user.ExpiryDate != null && user.ExpiryDate < DateTimeOffset.Now)
                    {
                        result = await _signInManager.CheckPasswordSignInAsync(user, userSignInSpec.Password, lockoutOnFailure: true).ConfigureAwait(false);

                        if (result.Succeeded)
                        {
                            throw new UnauthorizedApiException(UnauthorizedApiException.AccountExpired, "The user account is expired");
                        }
                    }

                    bool remember = userSignInSpec.RememberSet && userSignInSpec.Remember;

                    result = await _signInManager.PasswordSignInAsync(userSignInSpec.Email, userSignInSpec.Password, isPersistent: remember, lockoutOnFailure: true).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User signed in");

                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();

                        if (_userNotificationListener != null)
                        {
                            await _userNotificationListener.UserLoggedInAsync(user.Id).ConfigureAwait(false);
                        }

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

                        exception.UserUuid = user.Id;

                        throw exception;
                    }
                    else
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.InvalidCredentials, "The user credentials are not valid");
                    }
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when signing in user: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        // sign in through external authentication provider

        public async Task<(UserModel, bool)> SignInUserAsync(AuthenticateResult authenticateResult)
        {
            try
            {
                if (_tenantInfoAccessor == null)
                {
                    // this should not be possible

                    throw new Exception("Tenant info accessor is NULL for external authentication provider user signin");
                }

                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                var developerUuid = tenantInfo.DeveloperUuid;
                var tenantUuid = tenantInfo.TenantUuid;

                var targetTenantUuid = GetTargetTenantUuid(authenticateResult);

                if (!tenantInfo.OidcUseStateRedirect)
                {
                    targetTenantUuid = null;
                }

                var externalUser = authenticateResult.Principal;

                // try to determine the unique id of the external user(issued by the provider)
                // the most common claim type for that are the sub claim and the NameIdentifier
                // depending on the external provider, some other claim type might be used

                var claimMappings = tenantInfo.ExternalAuthenticationClaimMappings;

                if (claimMappings != null && claimMappings.Count > 0)
                {
                    // map claims

                    foreach (var claimMapping in claimMappings)
                    {
                        foreach (var identity in externalUser.Identities)
                        {
                            var existingTargetClaim = identity.FindFirst(claimMapping.Value);
                            if (existingTargetClaim == null)
                            {
                                var claimToMap = identity.FindFirst(claimMapping.Key);
                                if (claimToMap != null)
                                {
                                    identity.AddClaim(new Claim(claimMapping.Value, claimToMap.Value));
                                }
                            }
                        }
                    }
                }

                var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                    externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                    externalUser.FindFirst("nameidentifier");

                if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value) || userIdClaim.Value.Length > ApiImpl.MaxExternalUuidLength)
                {
                    PrintClaimsPrincipalDebug("Subject", externalUser);

                    throw new UnauthorizedApiException(UnauthorizedApiException.ExternalUserIdIsMissing, "The external user ID is missing or invalid");
                }

                var userIdClaimValue = userIdClaim.Value;

                // remove the user id claim so we don't include it as an extra claim if/when we provision the user

                var claims = externalUser.Claims.ToList();

                claims.Remove(userIdClaim);

                var clientSubClaims = externalUser.FindAll("client_sub");

                if (clientSubClaims != null && clientSubClaims.Any())
                {
                    // security-wise it is better to make sure nobody adds these to our DB

                    foreach (var clientSubClaim in clientSubClaims)
                    {
                        claims.Remove(clientSubClaim);
                    }
                }

                string unscopedEmailAddress = ProcessEmail(externalUser);

                string normalizedUnscopedEmailAddress = Normalize(unscopedEmailAddress);

                if (_devAdminSsoProtectedUserAccountEmailAddresses.Contains(normalizedUnscopedEmailAddress))
                {
                    // debug information to the console, without logging it to Sentry

                    Console.WriteLine("Dev admin SSO login diagnostics output:");

                    foreach (var claim in claims)
                    {
                        Console.WriteLine($"Dev admin login with claim {claim.Type}: {claim.Value}");
                    }

                    Console.WriteLine("SSO diagnostics output done");

                    string issuer = ProcessIssuer(externalUser);

                    if (!_devAdminSsoAuthorizedIssuers.Contains(issuer))
                    {
                        _logger.LogError($"Dev admin authorized issuers check failed for email address {normalizedUnscopedEmailAddress} and issuer {issuer}");

                        throw new Exception("The external authentication failed");
                    }
                }

                string scopedEmailAddress;

                if (tenantInfo.ExternalAuthenticationAllowUserMerge)
                {
                    scopedEmailAddress = unscopedEmailAddress;
                }
                else
                {
                    scopedEmailAddress = GetScopedEmail(developerUuid, tenantUuid, unscopedEmailAddress);
                }

                var user = await _userManager.FindByEmailAsync(scopedEmailAddress).ConfigureAwait(false);

                // stay outside of transaction

                if (user == null)
                {
                    user = await CreateUserAsync(developerUuid, tenantUuid, authenticateResult.Ticket, externalUser, userIdClaimValue, targetTenantUuid).ConfigureAwait(false);

                    return (user, true);
                }

                var scopedUserUuid = user.Id;

                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    user = await _userManager.FindByIdAsync(scopedUserUuid).ConfigureAwait(false);

                    if (user == null)
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.ExternalUserNotFound, "The external user was not found");
                    }

                    bool isLockedOut = await _userManager.IsLockedOutAsync(user).ConfigureAwait(false);

                    if (isLockedOut)
                    {
                        _logger.LogWarning("User account is locked out");

                        throw new UnauthorizedApiException(UnauthorizedApiException.AccountLockedOut, "The user account is locked out");
                    }

                    if (user.Disabled == true)
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.AccountDisabled, "The user account is disabled");
                    }

                    if (user.ExpiryDate != null && user.ExpiryDate < DateTimeOffset.Now)
                    {
                        throw new UnauthorizedApiException(UnauthorizedApiException.AccountExpired, "The user account is expired");
                    }

                    if (_configurationProvider.RequireEmailConfirmed && !user.EmailConfirmed)
                    {
                        var exception = new UnauthorizedApiException(UnauthorizedApiException.EmailNotConfirmed, "The email address is not yet confirmed");

                        exception.UserUuid = user.Id;

                        throw exception;
                    }

                    bool changed = false;

                    // update the user

                    string firstName = null;
                    string lastName = null;
                    string phoneNumber = null;

                    if (_configurationProvider.ManageName && _configurationProvider.RegisterName)
                    {
                        firstName = ProcessFirstName(externalUser);
                        lastName = ProcessLastName(externalUser);
                    }

                    if (_configurationProvider.ManagePhoneNumber && _configurationProvider.RegisterPhoneNumber)
                    {
                        phoneNumber = ProcessPhoneNumber(externalUser);
                    }

                    var identityToken = ProcessIdentityToken(authenticateResult.Ticket);
                    user.IdentityTokenCache = identityToken;

                    var accessToken = ProcessAccessToken(authenticateResult.Ticket);
                    var refreshToken = ProcessRefreshToken(authenticateResult.Ticket);

                    HashSet<string> memberOf = ProcessMemberOf(externalUser);

                    // Console.WriteLine($"Member of count {memberOf?.Count}: {memberOf}");

                    if (_configurationProvider.ManageName)
                    {
                        if (!string.Equals(user.FirstName, firstName))
                        {
                            user.FirstName = firstName;

                            changed = true;
                        }

                        if (!string.Equals(user.LastName, lastName))
                        {
                            user.LastName = lastName;

                            changed = true;
                        }
                    }

                    if (_configurationProvider.ManagePhoneNumber)
                    {
                        if (!string.Equals(user.PhoneNumber, phoneNumber))
                        {
                            user.PhoneNumber = phoneNumber;

                            changed = true;
                        }
                    }

                    if (!EqualsMemberOf(user.MemberOf, memberOf))
                    {
                        user.MemberOf = memberOf?.ToList();

                        changed = true;
                    }

                    if (!string.Equals(user.ExternalUuid, userIdClaimValue))
                    {
                        user.ExternalUuid = userIdClaimValue;

                        changed = true;
                    }

                    if (changed)
                    {
                        var updateResult = await _userManager.UpdateAsync(user).ConfigureAwait(false);

                        if (!updateResult.Succeeded)
                        {
                            HandleIdentityError(updateResult.Errors);
                        }

                        await _signInManager.RefreshSignInAsync(user).ConfigureAwait(false);

                        user = await _userManager.FindByIdAsync(scopedUserUuid).ConfigureAwait(false);
                    }

                    _signInManager.ClaimsFactory = new Saml2SupportClaimsFactory(_signInManager.ClaimsFactory, externalUser);

                    await _signInManager.SignInAsync(user, isPersistent: false).ConfigureAwait(false);
                    
                    _logger.LogInformation("External user signed in");

                    await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                    transaction.Commit();

                    if (_userNotificationListener != null)
                    {
                        await _userNotificationListener.UserLoggedInAsync(user.Id).ConfigureAwait(false);
                    }

                    // we need to always send the change notification, because maybe user groups
                    // in dependent systems have changed and we then need to fix user group assignments

                    await SendUserChangeNotificationAsync(user.Id, accessToken, refreshToken, targetTenantUuid, trySynchronousFirst: true).ConfigureAwait(false);

                    return (user, false);
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when signing in external user: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task<UserModel> ConfirmUserEmailAddressAsync(string userUuid, UserConfirmEmailSpec userConfirmEmailSpec)
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
                        throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found", userUuid);
                    }

                    if (user.EmailConfirmed)
                    {
                        // email already confirmed

                        throw new RequestFailedApiException(RequestFailedApiException.EmailAlreadyConfirmed, "The email address already has been confirmed");
                    }

                    var result = await _userManager.ConfirmEmailAsync(user, userConfirmEmailSpec.Code).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();

                        if (_userNotificationListener != null)
                        {
                            await _userNotificationListener.UserConfirmedEmailAsync(user.Id).ConfigureAwait(false);
                        }

                        await SendUserChangeNotificationAsync(user.Id).ConfigureAwait(false);

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
                _logger.LogError($"Error when confirming user email address: {e}");

                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when confirming user email address: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task UserForgotPasswordAsync(UserForgotPasswordSpec userForgotPasswordSpec, HttpRequest request = null, RecaptchaSettings recaptchaSettings = null)
        {
            userForgotPasswordSpec.Email = ProcessEmail(userForgotPasswordSpec.Email);

            if (_recaptchaService != null && request != null)
            {
                var recaptchaValidationResult = await _recaptchaService.Validate(request);

                if (!recaptchaValidationResult.success)
                    throw new RequestFailedApiException(RequestFailedApiException.RecaptchaValidationFailed, "The reCAPTCHA validation failed");
            }

            try
            {
                HandleEnforceExternalAuthenticationForEmailDomains(userForgotPasswordSpec.Email);

                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var user = await _userManager.FindByEmailAsync(userForgotPasswordSpec.Email).ConfigureAwait(false);

                    if (user == null)
                    {
                        // Don't reveal that the user does not exist or is not confirmed

                        _logger.LogWarning($"Password reset attempted for not existing email address: {userForgotPasswordSpec.Email}");

                        return;
                    }

                    var code = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);

                    var currentCultureInfo = CultureInfo.CurrentCulture;

                    var callbackUrl = _urlHelper.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { code, culture = currentCultureInfo.ToString() },
                        protocol: "https");

                    EmailTemplate emailTemplate = await _emailTemplateProvider.GetForgotPasswordEmailAsync(
                            new ForgotPasswordEmailViewModel(callbackUrl), isPortals: null, currentCultureInfo).ConfigureAwait(false);

                    await _emailSender.SendEmailAsync(userForgotPasswordSpec.Email, emailTemplate.Subject, emailTemplate.Body).ConfigureAwait(false);

                    await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                    transaction.Commit();

                    if (_userNotificationListener != null)
                    {
                        await _userNotificationListener.UserForgotPasswordAsync(user.Id).ConfigureAwait(false);
                    }
                }
            }
            catch (ApiException)
            {
                throw;
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

                    if (user.PasswordHash == null)
                    {
                        // externally managed

                        throw new RequestFailedApiException(RequestFailedApiException.AccountIsExternallyManaged, "This operation cannot be performed, because the user account is externally managed");
                    }

                    var result = await _userManager.ResetPasswordAsync(user, resetUserPasswordSpec.Code, resetUserPasswordSpec.Password).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();

                        if (_userNotificationListener != null)
                        {
                            await _userNotificationListener.UserResetPasswordAsync(user.Id).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        HandleIdentityError(result.Errors);
                    }
                }
            }
            catch (ApiException)
            {
                throw;
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
                        throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found", userUuid);
                    }

                    if (user.PasswordHash == null)
                    {
                        // externally managed

                        throw new RequestFailedApiException(RequestFailedApiException.AccountIsExternallyManaged, "This operation cannot be performed, because the user account is externally managed");
                    }

                    var changePasswordResult = await _userManager.ChangePasswordAsync(user, setUserPasswordSpec.OldPassword, setUserPasswordSpec.NewPassword).ConfigureAwait(false);

                    if (!changePasswordResult.Succeeded)
                    {
                        HandleIdentityError(changePasswordResult.Errors);
                    }

                    await _signInManager.RefreshSignInAsync(user).ConfigureAwait(false);

                    await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                    transaction.Commit();

                    if (_userNotificationListener != null)
                    {
                        await _userNotificationListener.UserSetPasswordAsync(user.Id).ConfigureAwait(false);
                    }
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when setting password: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task SignOutUserAsync(HttpContext httpContext)
        {
            await _signInManager.SignOutAsync().ConfigureAwait(false);

            string userUuid = null;
            UserModel userModel = null;

            if (httpContext.User != null)
            {
                userUuid = httpContext.User.GetUserUuid();

                if (!string.IsNullOrEmpty(userUuid))
                {
                    userModel = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);
                }
            }

            if (_tenantInfoAccessor != null)
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                var externalAuthenticationAllowLocalLogin = tenantInfo.ExternalAuthenticationAllowLocalLogin;

                var isRemoteUser = true;

                if (externalAuthenticationAllowLocalLogin)
                {
                    isRemoteUser = false;

                    if (userModel != null && (userModel.PasswordHash == null || !string.IsNullOrEmpty(userModel.IdentityTokenCache)))
                    {
                        // this is a remote user for sure

                        isRemoteUser = true;
                    }
                }

                if (isRemoteUser)
                {
                    if (string.Equals(tenantInfo.ExternalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodOidc))
                    {
                        string idTokenHint = null;

                        if (userModel != null)
                        {
                            idTokenHint = userModel.IdentityTokenCache;
                        }

                        if (!string.IsNullOrEmpty(idTokenHint))
                        {
                            httpContext.Items[IdentityCoreConstants.HttpContextItemsIdTokenHint] = idTokenHint;
                        }

                        await httpContext.SignOutAsync(IdentityCoreConstants.ExternalOidcScheme).ConfigureAwait(false);
                    }
                    else if (string.Equals(tenantInfo.ExternalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodSaml))
                    {
                        var webUrl = tenantInfo.WebUrl;

                        var props = new AuthenticationProperties
                        {
                            RedirectUri = webUrl,
                            Items =
                            {
                                { "returnUrl", webUrl }
                            }
                        };

                        await httpContext.SignOutAsync(IdentityCoreConstants.ExternalSamlScheme, props).ConfigureAwait(false);
                    }
                }
            }

            if (userModel != null)
            {
                await _userManager.UpdateSecurityStampAsync(userModel).ConfigureAwait(false);
            }

            _logger.LogInformation("User logged out");
           
            if (_userNotificationListener != null && !string.IsNullOrEmpty(userUuid))
            {
                await _userNotificationListener.UserLoggedOutAsync(userUuid).ConfigureAwait(false); 
            }
        }

        public async Task<UserModel> GetUserAsync(string userUuid)
        {
            userUuid = ProcessUserUuid(userUuid);

            try
            {
                var user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                if (user == null)
                {
                    throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found", userUuid);
                }

                return user;
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when getting user: {e}");

                throw new InternalServerErrorApiException();
            }
        }
        
        public async Task<UserModel> GetUserByEmailAsync(string emailAddress)
        {
            emailAddress = ProcessEmail(emailAddress);

            try
            {
                var user = await _userManager.FindByEmailAsync(emailAddress).ConfigureAwait(false);

                if (user == null)
                {
                    throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with email {emailAddress} was not found", emailAddress);
                }

                return user;
            }
            catch (ApiException)
            {
                throw;
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
                throw new RequestFailedApiException(RequestFailedApiException.UserDetailsNotSupported, "This system does not support managing user detail data");

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
                        throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found", userUuid);
                    }

                    bool changed = false;

                    string oldFirstName = null;
                    string oldLastName = null;
                    string oldPhoneNumber = null;
                    string oldNotificationCulture = null;
                    bool? oldGroupNotifications = null;
                    string oldCurrency = null;

                    string newFirstName = null;
                    string newLastName = null;
                    string newPhoneNumber = null;
                    string newNotificationCulture = null;
                    bool? newGroupNotifications = null;
                    string newCurrency = null;

                    if (_configurationProvider.ManageName)
                    {
                        if (!string.IsNullOrEmpty(userSpec.FirstName))
                        {
                            userSpec.FirstName = ProcessFirstName(userSpec.FirstName);

                            if (!string.Equals(oldUser.FirstName, userSpec.FirstName))
                            {
                                oldFirstName = oldUser.FirstName;

                                oldUser.FirstName = userSpec.FirstName;

                                newFirstName = oldUser.FirstName;

                                changed = true;
                            }
                        }

                        if (!string.IsNullOrEmpty(userSpec.LastName))
                        {
                            userSpec.LastName = ProcessLastName(userSpec.LastName);

                            if (!string.Equals(oldUser.LastName, userSpec.LastName))
                            {
                                oldLastName = oldUser.LastName;

                                oldUser.LastName = userSpec.LastName;

                                newLastName = oldUser.LastName;

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
                                oldPhoneNumber = oldUser.PhoneNumber;

                                oldUser.PhoneNumber = userSpec.PhoneNumber;

                                newPhoneNumber = oldUser.PhoneNumber;

                                changed = true;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(userSpec.NotificationCulture))
                    {
                        userSpec.NotificationCulture = ProcessNotificationCulture(userSpec.NotificationCulture)?.ToString();

                        if (!string.Equals(oldUser.NotificationCulture, userSpec.NotificationCulture))
                        {
                            oldNotificationCulture = oldUser.NotificationCulture;

                            oldUser.NotificationCulture = userSpec.NotificationCulture;

                            newNotificationCulture = oldUser.NotificationCulture;

                            changed = true;
                        }
                    }

                    if (userSpec.GroupNotifications != null)
                    {
                        userSpec.GroupNotifications = ProcessGroupNotifications(userSpec.GroupNotifications);

                        if (oldUser.GroupNotifications != userSpec.GroupNotifications)
                        {
                            oldGroupNotifications = oldUser.GroupNotifications;

                            oldUser.GroupNotifications = (bool)userSpec.GroupNotifications;

                            newGroupNotifications = oldUser.GroupNotifications;

                            changed = true;
                        }
                    }

                    if (!string.IsNullOrEmpty(userSpec.Currency))
                    {
                        userSpec.Currency = ProcessCurrency(userSpec.Currency)?.ToString();

                        if (!string.Equals(oldUser.Currency, userSpec.Currency))
                        {
                            oldCurrency = oldUser.Currency;

                            oldUser.Currency = userSpec.Currency;

                            newCurrency = oldUser.Currency;

                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        var updateResult = await _userManager.UpdateAsync(oldUser).ConfigureAwait(false);

                        if (!updateResult.Succeeded)
                        {
                            HandleIdentityError(updateResult.Errors);
                        }

                        await _signInManager.RefreshSignInAsync(oldUser).ConfigureAwait(false);

                        await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                        transaction.Commit();

                        if (_userNotificationListener != null)
                        {
                            var oldUserNotificationModel = new UserNotificationModel
                            {
                                FirstName = oldFirstName,
                                LastName = oldLastName,
                                PhoneNumber = oldPhoneNumber,
                                NotificationCulture = oldNotificationCulture,
                                GroupNotifications = oldGroupNotifications,
                                Currency = oldCurrency
                            };

                            var newUserNotificationModel = new UserNotificationModel
                            {
                                FirstName = newFirstName,
                                LastName = newLastName,
                                PhoneNumber = newPhoneNumber,
                                NotificationCulture = newNotificationCulture,
                                GroupNotifications = newGroupNotifications,
                                Currency = newCurrency
                            };

                            await _userNotificationListener.UserUpdatedAsync(oldUser.Id, oldUserNotificationModel, newUserNotificationModel).ConfigureAwait(false);
                        }

                        await SendUserChangeNotificationAsync(oldUser.Id).ConfigureAwait(false);                        
                    }
                }

                using (var transaction = await _identityDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
                {
                    var newUser = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                    return newUser;
                }
            }
            catch (ApiException)
            {
                throw;
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
                        throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found", userUuid);
                    }

                    if (user.EmailConfirmed)
                    {
                        // email already confirmed

                        throw new RequestFailedApiException(RequestFailedApiException.EmailAlreadyConfirmed, "The email address already has been confirmed");
                    }

                    var numberOfEmailConfirmationAttempts = user.NumberOfEmailConfirmationAttempts;

                    if (numberOfEmailConfirmationAttempts > 3)
                    {
                        throw new RequestFailedApiException(RequestFailedApiException.TooManyEmailConfirmationAttempts, "Too many email confirmation attempts have been recorded. Please get in touch with us to look at the issue");
                    }
                    
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);

                    var currentCultureInfo = CultureInfo.CurrentCulture;

                    var callbackUrl = _urlHelper.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { userUuid = user.Id, code, culture = currentCultureInfo.ToString() },
                        protocol: "https");

                    EmailTemplate emailTemplate = await _emailTemplateProvider.GetConfirmAccountEmailAsync(
                        new ConfirmAccountEmailViewModel(callbackUrl), isPortals: null, currentCultureInfo).ConfigureAwait(false);

                    await _emailSender.SendEmailAsync(user.GetEmail(), emailTemplate.Subject, emailTemplate.Body).ConfigureAwait(false);

                    user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                    user.NumberOfEmailConfirmationAttempts++;

                    await _userManager.UpdateAsync(user).ConfigureAwait(false);

                    await _identityDbContext.SaveChangesAsync().ConfigureAwait(false);

                    transaction.Commit();

                    if (_userNotificationListener != null)
                    {
                        await _userNotificationListener.UserResentEmailConfirmationEmailAsync(user.Id).ConfigureAwait(false);
                    }
                }
            }
            catch (ApiException e)
            {
                _logger.LogError($"Error when resending email confirmation email: {e}");

                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when resending email confirmation email: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        private void HandleEnforceExternalAuthenticationForEmailDomains(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return;
            }

            var tenantInfo = _tenantInfoAccessor.TenantInfo;

            if (tenantInfo != null && 
                tenantInfo.EnforceExternalAuthenticationForEmailDomains != null && 
                tenantInfo.EnforceExternalAuthenticationForEmailDomains.Any())
            {
                if (tenantInfo.EnforceExternalAuthenticationForEmailDomains.Any(enforceExternalAuthenticationForEmailDomain =>
                    email.EndsWith($"@{enforceExternalAuthenticationForEmailDomain}", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new RequestFailedApiException(RequestFailedApiException.ExternalAuthenticationRequiredForEmailDomain, "For this email domain external authentication is required");
                }
            }
        }

        public Task AcceptTermsAsync(string userUuid, string code, UserAcceptTermsSpec userAcceptTermsSpec)
        {
            throw new NotImplementedException();
        }

        private string ProcessUserUuid(string userUuid)
        {
            if (string.IsNullOrEmpty(userUuid))
                throw new RequestFailedApiException(RequestFailedApiException.UserUuidMissing, "The user UUID is missing");

            if (!ApiImpl.Uuid.IsMatch(userUuid))
                throw new RequestFailedApiException(RequestFailedApiException.UserUuidInvalid, "The user UUID is invalid");

            if (userUuid.Length > ApiImpl.MaxUserUuidLength)
                throw new RequestFailedApiException(RequestFailedApiException.UserUuidTooLong, "The user UUID is too long");

            return userUuid;
        }

        private string ProcessEmail(string email)
        {
            email = email?.Trim();

            if (string.IsNullOrEmpty(email))
                throw new RequestFailedApiException(RequestFailedApiException.EmailMissing, "The email address is missing");

            if (!ApiImpl.SafeString.IsMatch(email))
                throw new RequestFailedApiException(RequestFailedApiException.EmailInvalid, $"The email address is invalid");

            if (!new EmailAddressAttribute().IsValid(email))
                throw new RequestFailedApiException(RequestFailedApiException.EmailInvalid, "The email address is invalid");

            if (email.Length > ApiImpl.MaxEmailAddressLength)
                throw new RequestFailedApiException(RequestFailedApiException.EmailInvalid, "The email address is too long");

            return email;
        }

        private string ProcessEmail(ClaimsPrincipal claimsPrincipal)
        {
            var emailClaim = claimsPrincipal.FindFirst(JwtClaimTypes.Email) ??
                   claimsPrincipal.FindFirst(ClaimTypes.Email) ??
                   claimsPrincipal.FindFirst("EmailAddress"); // from SSO Circle

            if (emailClaim == null)
            {
                PrintClaimsPrincipalDebug("Email address", claimsPrincipal);

                throw new RequestFailedApiException(RequestFailedApiException.EmailMissing, "The email address is missing");
            }

            string email = emailClaim.Value;

            return ProcessEmail(email);
        }

        private string ProcessIssuer(ClaimsPrincipal claimsPrincipal)
        {
            var issuerClaim = claimsPrincipal.FindFirst("issuer");

            if (issuerClaim == null)
                throw new Exception("The external authentication failed");

            string issuer = issuerClaim.Value;

            if (string.IsNullOrEmpty(issuer))
                throw new Exception("The external authentication failed");

            return issuer;
        }

        private bool ProcessEmailConfirmed(ClaimsPrincipal claimsPrincipal)
        {
            var emailConfirmedClaim = claimsPrincipal.FindFirst(JwtClaimTypes.EmailVerified);

            if (emailConfirmedClaim == null)
                return false;

            string emailConfirmedValue = emailConfirmedClaim.Value;

            return string.Equals(emailConfirmedValue, "true");
        }

        private string ProcessFirstName(string firstName)
        {
            firstName = ApiImpl.CleanToSafeString(firstName);

            if (string.IsNullOrEmpty(firstName))
                throw new RequestFailedApiException(RequestFailedApiException.FirstNameMissing, "The first name is missing");
            
            if (firstName.Length > ApiImpl.MaxFirstNameLength)
                throw new RequestFailedApiException(RequestFailedApiException.FirstNameTooLong, "The fist name is too long");

            return firstName;
        }

        private string ProcessFirstName(ClaimsPrincipal claimsPrincipal)
        {
            var firstNameClaim = claimsPrincipal.FindFirst(JwtClaimTypes.GivenName) ??
                   claimsPrincipal.FindFirst(ClaimTypes.GivenName) ??
                   claimsPrincipal.FindFirst("FirstName"); // from SSO Circle

            if (firstNameClaim == null)
            {
                PrintClaimsPrincipalDebug("First name", claimsPrincipal);

                return null;
            }

            string firstName = firstNameClaim.Value;

            return ProcessFirstName(firstName);
        }

        private string ProcessLastName(string lastName)
        {
            lastName = ApiImpl.CleanToSafeString(lastName);

            if (string.IsNullOrEmpty(lastName))
                throw new RequestFailedApiException(RequestFailedApiException.LastNameMissing, "The last name is missing");
            
            if (lastName.Length > ApiImpl.MaxLastNameLength)
                throw new RequestFailedApiException(RequestFailedApiException.LastNameTooLong, "The last name is too long");

            return lastName;
        }

        private string ProcessLastName(ClaimsPrincipal claimsPrincipal)
        {
            var lastNameClaim = claimsPrincipal.FindFirst(JwtClaimTypes.FamilyName) ??
                   claimsPrincipal.FindFirst(ClaimTypes.Surname) ??
                   claimsPrincipal.FindFirst("LastName"); // from SSO Circle

            if (lastNameClaim == null)
            {
                PrintClaimsPrincipalDebug("Last name", claimsPrincipal);

                return null;
            }

            string lastName = lastNameClaim.Value;

            return ProcessLastName(lastName);
        }

        private string ProcessPhoneNumber(string phoneNumber)
        {
            phoneNumber = phoneNumber?.Trim();

            if (string.IsNullOrEmpty(phoneNumber))
                throw new RequestFailedApiException(RequestFailedApiException.PhoneNumberMissing, "The phone number is missing");
            
            if (!new PhoneAttribute().IsValid(phoneNumber))
                throw new RequestFailedApiException(RequestFailedApiException.PhoneNumberInvalid, "The phone number is invalid");

            if (phoneNumber.Length > ApiImpl.MaxPhoneNumberLength)
                throw new RequestFailedApiException(RequestFailedApiException.PhoneNumberTooLong, $"The phone number is too long");

            return phoneNumber;
        }

        private string ProcessPhoneNumber(ClaimsPrincipal claimsPrincipal)
        {
            var phoneNumberClaim = claimsPrincipal.FindFirst(JwtClaimTypes.PhoneNumber) ??
                   claimsPrincipal.FindFirst(ClaimTypes.HomePhone) ??
                   claimsPrincipal.FindFirst(ClaimTypes.MobilePhone) ??
                   claimsPrincipal.FindFirst(ClaimTypes.OtherPhone) ??
                   claimsPrincipal.FindFirst("PhoneNumber"); // (unconfirmed)

            if (phoneNumberClaim == null)
            {
                PrintClaimsPrincipalDebug("Phone number", claimsPrincipal);

                throw new RequestFailedApiException(RequestFailedApiException.PhoneNumberMissing, "The phone number is missing");
            }

            string phoneNumber = phoneNumberClaim.Value;

            return ProcessPhoneNumber(phoneNumber);
        }

        private void PrintClaimsPrincipalDebug(string debugHint, ClaimsPrincipal claimsPrincipal)
        {
            /*
            
            var claims = claimsPrincipal.Claims;

            Console.WriteLine("---");
            Console.WriteLine($"Searching for claim {debugHint}, but not found in claim collection:");

            foreach (var claim in claims)
            {
                Console.WriteLine($"{claim.Type}: '{claim.Value}'");
            }

            Console.WriteLine("---");

            */
        }

        private HashSet<string> ProcessMemberOf(ClaimsPrincipal claimsPrincipal)
        {
            var result = new HashSet<string>();

            ProcessMemberOfClaims(claimsPrincipal, "member", result);
            ProcessMemberOfClaims(claimsPrincipal, "memberOf", result);
            ProcessMemberOfClaims(claimsPrincipal, "member-of", result);
            ProcessMemberOfClaims(claimsPrincipal, "groups", result);

            // Azure AD default

            ProcessMemberOfClaims(claimsPrincipal, "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups", result);

            if (result.Count == 0)
                return null;

            return result;
        }

        private bool EqualsMemberOf(List<string> oldMemberOf, HashSet<string> newMemberOf)
        {
            if (oldMemberOf == null && newMemberOf == null)
                return true;

            if (oldMemberOf == null && newMemberOf != null)
                return false;

            if (oldMemberOf != null && newMemberOf == null)
                return false;

            if (oldMemberOf.Any(oldMemberOfValue => !newMemberOf.Any(newMemberOfValue => string.Equals(oldMemberOfValue, newMemberOfValue))))
                return false;

            if (newMemberOf.Any(newMemberOfValue => !oldMemberOf.Any(oldMemberOfValue => string.Equals(oldMemberOfValue, newMemberOfValue))))
                return false;

            return true;
        }

        private void ProcessMemberOfClaims(ClaimsPrincipal claimsPrincipal, string claimName, HashSet<string> result)
        {
            var claims = claimsPrincipal.Claims;

            foreach (var claim in claims)
            {
                // Console.WriteLine($"Checking claim {claim.Type} with value {claim.Value}");

                if (string.Equals(claim.Type, "http://schemas.microsoft.com/claims/groups.link"))
                {
                    // will be added by Azure AD if there is > 150 user groups for the user
                    // the user then will not contain any group claims anymore

                    throw new RequestFailedApiException(RequestFailedApiException.ClaimsPrincipalHasTooManyGroups, "The claims principal used to log in has > 150 groups, which is not supported by Azure AD. Please either limit the amount of user groups assigned to the user, or use application roles");
                }

                if (string.Equals(claim.Type, claimName, StringComparison.OrdinalIgnoreCase))
                {
                    // Console.WriteLine("Does match");

                    string value = claim.Value;

                    value = value?.Trim();

                    if (!string.IsNullOrEmpty(value))
                    {
                        result.Add(value);

                        // Console.WriteLine($"{value} was added");
                    }
                }
                else
                {
                    // Console.WriteLine("Does not match");
                }
            }
        }

        private string ProcessPreferredUserName(ClaimsPrincipal claimsPrincipal)
        {
            var preferredUserNameClaim = claimsPrincipal.FindFirst(JwtClaimTypes.PreferredUserName);

            if (preferredUserNameClaim == null)
                return null;

            string preferredUserName = preferredUserNameClaim.Value;

            if (string.IsNullOrEmpty(preferredUserName))
                return null;

            return preferredUserName;
        }

        private CultureInfo ProcessNotificationCulture(string notificationCulture)
        {
            if (string.IsNullOrEmpty(notificationCulture))
                return null;

            try
            {
                return CultureInfo.GetCultureInfo(notificationCulture);
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.NotificationCultureInvalid, "The notification culture is invalid");
            }
        }

        private bool ProcessGroupNotifications(bool? groupNotifications)
        {
            return groupNotifications ?? true;
        }

        private string ProcessCurrency(string currency)
        {
            if (string.IsNullOrEmpty(currency))
                return null;

            if (string.Equals(currency, "eur") ||
                string.Equals(currency, "usd"))
            {
                return currency;
            }

            throw new RequestFailedApiException(RequestFailedApiException.CurrencyInvalid, "The currency is invalid");
        }

        private string ProcessPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new RequestFailedApiException(RequestFailedApiException.PasswordMissing, "The password is missing");
            
            if (password.Length < ApiImpl.MinPasswordLength)
                throw new RequestFailedApiException(RequestFailedApiException.PasswordTooShort, "The password is too short");

            if (password.Length > ApiImpl.MaxPasswordLength)
                throw new RequestFailedApiException(RequestFailedApiException.PasswordTooLong, "The password is too long");

            return password;
        }

        private string ProcessPasswordConfirmation(string password, string passwordConfirmation)
        {
            if (string.IsNullOrEmpty(passwordConfirmation))
                throw new RequestFailedApiException(RequestFailedApiException.PasswordConfirmationMissing, "The password confirmation is missing");

            if (!string.Equals(password, passwordConfirmation))
                throw new RequestFailedApiException(RequestFailedApiException.PasswordConfirmationNoMatch, "The password confirmation is not matching the password");

            return password;
        }

        private string ProcessCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new RequestFailedApiException(RequestFailedApiException.CodeMissing, "The code is missing");

            if (!ApiImpl.SafeString.IsMatch(code))
                throw new RequestFailedApiException(RequestFailedApiException.CodeInvalid, "The code contains invalid characters");

            if (code.Length > MaxCodeLength)
                throw new RequestFailedApiException(RequestFailedApiException.CodeTooLong, "The code is too long");

            return code;
        }

        private string ProcessAccessToken(AuthenticationTicket authenticationTicket)
        {
            if (authenticationTicket == null || authenticationTicket.Properties == null)
                return null;

            var accessToken = authenticationTicket.Properties.GetTokenValue("access_token");
            if (string.IsNullOrEmpty(accessToken))
                return null;

            if (accessToken.Length > ApiImpl.MaxAccessTokenLength)
                return null;

            return accessToken;
        }

        private string ProcessIdentityToken(AuthenticationTicket authenticationTicket)
        {
            if (authenticationTicket == null || authenticationTicket.Properties == null)
                return null;

            var identityToken = authenticationTicket.Properties.GetTokenValue("id_token");
            if (string.IsNullOrEmpty(identityToken))
                return null;

            if (identityToken.Length > ApiImpl.MaxIdentityTokenLength)
                throw new RequestFailedApiException(RequestFailedApiException.IdentityTokenTooLong, "The identity token is too long");

            return identityToken;
        }

        private string ProcessRefreshToken(AuthenticationTicket authenticationTicket)
        {
            if (authenticationTicket == null || authenticationTicket.Properties == null)
                return null;

            var refreshToken = authenticationTicket.Properties.GetTokenValue("refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
                return null;

            if (refreshToken.Length > ApiImpl.MaxRefreshTokenLength)
                return null;

            return refreshToken;
        }

        private string GetDefaultCurrency()
        {
            if (_tenantInfoAccessor != null)
            {
                var tenantInfo = _tenantInfoAccessor.TenantInfo;

                if (tenantInfo != null)
                {
                    return tenantInfo.DefaultCurrency;
                }
            }

            return "eur";
        }

        private void HandleIdentityError(IEnumerable<IdentityError> errors)
        {
            var enumerator = errors.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var error = enumerator.Current;

                if (string.Equals(error.Code, "DuplicateUserName"))
                    throw new RequestFailedApiException(RequestFailedApiException.DuplicateUserName, "This user name already exists");
                else if (string.Equals(error.Code, "PasswordRequiresNonAlphanumeric"))
                    throw new RequestFailedApiException(RequestFailedApiException.PasswordRequiresNonAlphanumeric, "The password requires non alphanumeric characters");
                else if (string.Equals(error.Code, "PasswordRequiresDigit"))
                    throw new RequestFailedApiException(RequestFailedApiException.PasswordRequiresDigit, "The password requires digits");
                else if (string.Equals(error.Code, "PasswordRequiresLower"))
                    throw new RequestFailedApiException(RequestFailedApiException.PasswordRequiresLower, "The password requires lower case characters");
                else if (string.Equals(error.Code, "PasswordRequiresUpper"))
                    throw new RequestFailedApiException(RequestFailedApiException.PasswordRequiresUpper, "The password requires upper case characters");
                else if (string.Equals(error.Code, "PasswordTooShort"))
                    throw new RequestFailedApiException(RequestFailedApiException.PasswordTooShort, "The password is too short");
                else if (string.Equals(error.Code, "InvalidToken"))
                    throw new RequestFailedApiException(RequestFailedApiException.SecurityTokenInvalid, "The security token is invalid or expired");
                else if (string.Equals(error.Code, "PasswordMismatch"))
                    throw new UnauthorizedApiException(UnauthorizedApiException.PasswordDoesNotMatch, "The password does not match our records");

                _logger.LogWarning($"Identity error was not covered: {error.Code}");
            }
            else
            {
                _logger.LogWarning("Unknown identity error occured");
            }

            throw new InternalServerErrorApiException();
        }

        private async Task SendUserChangeNotificationAsync(string userUuid, string accessTokenCache = null, string refreshTokenCache = null, long? targetTenantUuid = null, bool trySynchronousFirst = false)
        {
            if (!string.IsNullOrEmpty(_configurationProvider.IdentityChangeTasksAmqpAddress))
            {
                // notification to dependent services

                string identityChangeTasksAmqpAddress = _configurationProvider.IdentityChangeTasksAmqpAddress;

                var identityChangeTask = new IdentityChangeTask()
                {
                    TenantUuid = targetTenantUuid,
                    UserUuid = userUuid,
                    AccessTokenCache = accessTokenCache,
                    RefreshTokenCache = refreshTokenCache
                };

                var amqpMessenger = _serviceProvider.GetRequiredService<IAMQPMessenger>();

                if (!trySynchronousFirst)
                    await amqpMessenger.SendMessageAsync(identityChangeTasksAmqpAddress, identityChangeTask).ConfigureAwait(false);
                else
                    await amqpMessenger.SendMessageTrySynchronousFirstAsync(identityChangeTasksAmqpAddress, identityChangeTask).ConfigureAwait(false);
            }
        }

        private long? GetTargetTenantUuid(AuthenticateResult authenticateResult)
        {
            var items = authenticateResult?.Properties?.Items;

            if (items == null)
            {
                return null;
            }

            if (!authenticateResult.Properties.Items.TryGetValue("targetTenantUuid", out string targetTenantUuidString))
            {
                return null;
            }

            if (string.IsNullOrEmpty(targetTenantUuidString) || !long.TryParse(targetTenantUuidString, out long targetTenantUuid))
            {
                return null;
            }

            return targetTenantUuid;
        }

        private async Task<UserDeletedModel> GetUserDeletedAsync(string externalUuid)
        {
            IQueryable<UserDeletedModel> query = _identityDbContext.UsersDeleted;

            query = query.Where(userDeleted => userDeleted.ExternalUuid == externalUuid);

            var userDeletedModel = await query.FirstOrDefaultAsync().ConfigureAwait(false);

            return userDeletedModel;
        }

        private string GetScopePrefix(long? developerUuid, long? tenantUuid)
        {
            if (developerUuid == null && tenantUuid == null)
            {
                return "";
            }

            return $"{developerUuid}{IdentityCoreConstants.UuidSeparator}{tenantUuid}{IdentityCoreConstants.UuidSeparator}";
        }

        private string GetScopedEmail(long? developerUuid, long? tenantUuid, string unscopedEmail)
        {
            return $"{GetScopePrefix(developerUuid, tenantUuid)}{unscopedEmail}";
        }

        private string GetNewScopedUuid(long? developerUuid, long? tenantUuid)
        {
            return $"{GetScopePrefix(developerUuid, tenantUuid)}{Guid.NewGuid()}";
        }

        private string Normalize(string input)
        {
            return input?.Trim().ToUpperInvariant();
        }
    }
}
