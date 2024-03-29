﻿using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using reCAPTCHA.AspNetCore;

namespace HCore.Identity.Services
{
    public interface IIdentityServices
    {
        Task<UserModel> CreateUserAsync(UserSpec userSpec, bool isSelfRegistration, bool emailIsAlreadyConfirmed = false, HttpRequest request = null, bool requiresRecaptcha = true, RecaptchaSettings recaptcha = null, bool ignoreSelfRegistrationRestriction = false);
        Task<UserModel> ConfirmUserEmailAddressAsync(string userUuid, UserConfirmEmailSpec userConfirmEmailSpec);
        Task ResendUserEmailConfirmationEmailAsync(string userUuid);
        Task AcceptTermsAsync(string userUuid, string code, UserAcceptTermsSpec userAcceptTermsSpec);

        Task UserForgotPasswordAsync(UserForgotPasswordSpec userForgotPasswordSpec, HttpRequest request = null, RecaptchaSettings recaptchaSettings = null);
        Task ResetUserPasswordAsync(ResetUserPasswordSpec resetUserPasswordSpec);
        Task SetUserPasswordAsync(string userUuid, SetUserPasswordSpec setUserPasswordSpec);

        Task<UserModel> SignInUserAsync(UserSignInSpec userSignInSpec);
        Task<(UserModel, bool)> SignInUserAsync(AuthenticateResult authenticateResult);

        Task SignOutUserAsync(HttpContext httpContext);

        Task<UserModel> GetUserAsync(string userUuid);
        Task<UserModel> GetUserByEmailAsync(string emailAddress);

        Task<UserModel> UpdateUserAsync(string userUuid, UserSpec user, bool isAdmin);

        Task<ReservedEmailAddressModel> ReserveUserUuidAsync(long? developerUuid, long? tenantUuid, string emailAddress, bool processEmailAddress = true, bool createReservationIfNotPresent = true);
    }
}
