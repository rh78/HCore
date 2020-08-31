using HCore.Web.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace HCore.Web.API.Impl
{
    public class ApiImpl
    {
        public static readonly Regex Uuid = new Regex(@"^[a-zA-Z0-9\.@_\-\+\:\{\}]+$");
        public static readonly Regex SafeString = new Regex(@"^[\w\s\.@_\-\+\=:/]+$");

        public static readonly CultureInfo DefaultCultureInfo = CultureInfo.GetCultureInfo("en-US");

        public const int MaxScrollUuidLength = 50;

        public const int MaxUserUuidLength = 100;
        
        public const int MaxExternalUuidLength = 100;

        public const int MaxExternalUserGroupUuidLength = 100;

        public const int MaxEmailAddressLength = 50;
        public const int MaxPhoneNumberLength = 255;

        public const int MaxAddressLineLength = 50;
        public const int MaxPostalCodeLength = 10;
        public const int MaxCityLength = 50;
        public const int MaxStateLength = 50;
        public const int MaxVatIdLength = 15;

        public const int MaxUserNameLength = 50;
        public const int MaxFirstNameLength = 255;
        public const int MaxLastNameLength = 255;
        public const int MaxDisplayNameLength = MaxFirstNameLength + MaxLastNameLength + 1;
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 50;

        public const int MaxLocalizedStringLength = 255;

        public const int MaxCodeLength = 2048;

        public const int MaxNameLength = 50;
        public const int MaxShortDescriptionLength = 50;
        public const int MaxLabelLength = 50;
        public const int MaxSubmissionMessageLength = 512;
        public const int MaxCancellationReasonLength = 512;
        public const int MaxLongDescriptionLength = 65536;
        public const int MaxConfirmationIdLength = 50;

        public const int MaxClientIdLength = 255;
        public const int MaxClientSecretLength = 255;
        public const int MaxKey1Length = 255;
        public const int MaxKey2Length = 255;
        public const int MaxKey3Length = 255;
        public const int MaxKey4Length = 255;
        public const int MaxUrlLength = 255;
        public const int MaxRedirectUrlLength = 255;
        public const int MaxSecretLength = 50;
        public const int MaxAccessTokenLength = 2048;
        public const int MaxRefreshTokenLength = 2048;
        public const int MaxIssuerLength = 50;
        public const int MaxSubjectLength = 50;
        public const int MaxPrivateKeyLength = 5096;
        public const int MaxErrorCodeLength = 255;
        public const int MaxErrorMessageLength = 255;

        public const int MaxContactPersonNameLength = MaxFirstNameLength + MaxLastNameLength + 1; // including space

        public const int MaxCommentLength = 255;
        public const int MaxOrderIdLength = 50;

        public const int MaxSearchTermLength = 50;

        public const int MaxBulkUpdateCount = 50;

        public const int MaxIdentifierLength = 255;

        public static long? ProcessUserGroupUuid(string userGroupUuid, bool required)
        {
            if (string.IsNullOrEmpty(userGroupUuid))
            {
                if (required)
                    throw new RequestFailedApiException(RequestFailedApiException.UserGroupUuidMissing, "The user group UUID is missing");

                return null;
            }

            try
            {
                return Convert.ToInt64(userGroupUuid);
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.UserGroupUuidInvalid, "The user group UUID is invalid");
            }
        }

        public static HashSet<long?> ProcessUserGroupUuids(List<string> userGroupUuids)
        {
            if (userGroupUuids == null)
                return new HashSet<long?>();

            return userGroupUuids.Select(userGroupUuid => ProcessUserGroupUuid(userGroupUuid, true)).ToHashSet();
        }

        public static string ProcessUserUuid(string userUuid)
        {
            if (string.IsNullOrEmpty(userUuid))
                throw new RequestFailedApiException(RequestFailedApiException.UserUuidMissing, "The user UUID is missing");

            if (!SafeString.IsMatch(userUuid))
                throw new RequestFailedApiException(RequestFailedApiException.UserUuidInvalid, "The user UUID contains invalid characters");

            if (userUuid.Length > MaxExternalUuidLength)
                throw new RequestFailedApiException(RequestFailedApiException.UserUuidTooLong, "The user UUID is too long");

            return userUuid;
        }

        public static HashSet<string> ProcessUserUuids(List<string> userUuids)
        {
            if (userUuids == null)
                return new HashSet<string>();

            return userUuids.Select(userUuid => ProcessUserUuid(userUuid)).ToHashSet();
        }

        public static string ProcessEmailAddress(string emailAddress)
        {
            emailAddress = emailAddress?.Trim();

            if (string.IsNullOrEmpty(emailAddress))
                return null;

            if (!SafeString.IsMatch(emailAddress))
                throw new RequestFailedApiException(RequestFailedApiException.EmailInvalid, $"The email address is invalid");

            if (emailAddress.Length > MaxEmailAddressLength)
                throw new RequestFailedApiException(RequestFailedApiException.EmailTooLong, $"The email address is too long");

            return emailAddress;
        }

        public static string ProcessEmailAddressStrict(string email, bool required)
        {
            email = email?.Trim();

            if (string.IsNullOrEmpty(email))
            {
                if (required)
                    throw new RequestFailedApiException(RequestFailedApiException.EmailMissing, "The email address is missing");

                return null;
            }

            if (!new EmailAddressAttribute().IsValid(email))
                throw new RequestFailedApiException(RequestFailedApiException.EmailInvalid, "The email address is invalid");

            if (email.Length > MaxEmailAddressLength)
                throw new RequestFailedApiException(RequestFailedApiException.EmailInvalid, "The email address is too long");

            return email;
        }

        public static string ProcessAddressLine1(string addressLine1, bool isRequired)
        {
            addressLine1 = addressLine1?.Trim();

            if (string.IsNullOrEmpty(addressLine1))
            {
                if (isRequired)
                    throw new RequestFailedApiException(RequestFailedApiException.AddressLine1Missing, "The address line 1 is missing");

                return null;
            }

            if (addressLine1.Length > MaxAddressLineLength)
                throw new RequestFailedApiException(RequestFailedApiException.AddressLine1TooLong, "The address line 1 is too long");

            return addressLine1;
        }

        public static string ProcessAddressLine2(string addressLine2)
        {
            addressLine2 = addressLine2?.Trim();

            if (string.IsNullOrEmpty(addressLine2))
                return null;

            if (addressLine2.Length > MaxAddressLineLength)
                throw new RequestFailedApiException(RequestFailedApiException.AddressLine2TooLong, "The address line 2 is too long");

            return addressLine2;
        }

        public static string ProcessPostalCode(string postalCode, bool isRequired)
        {
            postalCode = postalCode?.Trim();

            if (string.IsNullOrEmpty(postalCode))
            {
                if (isRequired)
                    throw new RequestFailedApiException(RequestFailedApiException.PostalCodeMissing, "The postal code is missing");

                return null;
            }

            if (postalCode.Length > MaxPostalCodeLength)
                throw new RequestFailedApiException(RequestFailedApiException.PostalCodeTooLong, "The postal code is too long");

            return postalCode;
        }

        public static string ProcessCity(string city, bool isRequired)
        {
            city = city?.Trim();

            if (string.IsNullOrEmpty(city))
            {
                if (isRequired)
                    throw new RequestFailedApiException(RequestFailedApiException.CityMissing, "The city is missing");

                return null;
            }

            if (city.Length > MaxCityLength)
                throw new RequestFailedApiException(RequestFailedApiException.CityTooLong, "The city is too long");

            return city;
        }

        public static string ProcessState(string state)
        {
            state = state?.Trim();

            if (string.IsNullOrEmpty(state))
                return null;

            if (state.Length > MaxStateLength)
                throw new RequestFailedApiException(RequestFailedApiException.StateTooLong, "The state is too long");

            return state;
        }

        public static string ProcessVatIdUnsafe(string country, string vatId, bool isRequired)
        {
            vatId = vatId?.Trim();

            if (string.IsNullOrEmpty(country))
            {
                if (isRequired)
                    throw new RequestFailedApiException(RequestFailedApiException.CountryMissing, "The country is missing");

                return null;
            }

            if (string.IsNullOrEmpty(vatId))
            {
                if (isRequired)
                    throw new RequestFailedApiException(RequestFailedApiException.VatIdMissing, "The VAT ID is missing");

                return null;
            }

            if (vatId.Length > MaxVatIdLength)
                throw new RequestFailedApiException(RequestFailedApiException.VatIdTooLong, "The VAT ID is too long");

            return vatId;
        }

        public static string ProcessContactPersonName(string name, bool isRequired)
        {
            name = name?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                if (isRequired)
                    throw new RequestFailedApiException(RequestFailedApiException.ContactPersonNameMissing, "The contact person name is missing");

                return null;
            }

            if (name.Length > MaxContactPersonNameLength)
                throw new RequestFailedApiException(RequestFailedApiException.ContactPersonNameTooLong, "The contact person name is too long");

            return name;
        }

        public static CultureInfo ProcessNotificationCulture(string notificationCulture)
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

        public static bool ProcessGroupNotifications(bool? groupNotifications)
        {
            return groupNotifications ?? true;
        }

        public static string ProcessScrollUuid(string scrollUuid)
        {
            if (string.IsNullOrEmpty(scrollUuid))
                return null;

            if (!Uuid.IsMatch(scrollUuid))
                throw new RequestFailedApiException(RequestFailedApiException.ScrollUuidInvalid, "The scroll UUID is invalid");

            if (scrollUuid.Length > MaxScrollUuidLength)
                throw new RequestFailedApiException(RequestFailedApiException.ScrollUuidTooLong, "The scroll UUID is too long");

            return scrollUuid;
        }

        public static long? ProcessContinuationUuid(string continuationUuid)
        {
            if (string.IsNullOrEmpty(continuationUuid))
                return null;

            try
            {
                return Convert.ToInt64(continuationUuid);
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.ContinuationUuidInvalid, "The continuation UUID is invalid");
            }
        }

        public static long? ProcessTenantUuid(string tenantUuid)
        {
            if (string.IsNullOrEmpty(tenantUuid))
                throw new RequestFailedApiException(RequestFailedApiException.TenantUuidMissing, "The tenant UUID is missing");

            try
            {
                return Convert.ToInt64(tenantUuid);
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.TenantUuidInvalid, "The tenant UUID is invalid");
            }
        }

        public static long? ProcessScopedTenantUuid(string scopedTenantUuid)
        {
            if (string.IsNullOrEmpty(scopedTenantUuid))
                return null;

            try
            {
                return Convert.ToInt64(scopedTenantUuid);
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.ScopedTenantUuidInvalid, "The scoped tenant UUID is invalid");
            }
        }

        public static string ProcessUserName(string userName)
        {
            userName = userName?.Trim();

            if (string.IsNullOrEmpty(userName))
                throw new RequestFailedApiException(RequestFailedApiException.UserNameMissing, "The user name is missing");

            if (!SafeString.IsMatch(userName))
                throw new RequestFailedApiException(RequestFailedApiException.UserNameInvalid, "The user name contains invalid characters");

            if (userName.Length > MaxUserNameLength)
                throw new RequestFailedApiException(RequestFailedApiException.UserNameTooLong, "The user name is too long");

            return userName;
        }

        public static string ProcessPassword(string password)
        {
            password = password?.Trim();

            if (string.IsNullOrEmpty(password))
                throw new RequestFailedApiException(RequestFailedApiException.PasswordMissing, "The password is missing");

            if (password.Length > MaxPasswordLength)
                throw new RequestFailedApiException(RequestFailedApiException.PasswordTooLong, "The password is too long");

            return password;
        }

        public static string ProcessClientId(string clientId)
        {
            clientId = clientId?.Trim();

            if (string.IsNullOrEmpty(clientId))
                throw new RequestFailedApiException(RequestFailedApiException.ClientIdMissing, "The client ID is missing");

            if (!ApiImpl.SafeString.IsMatch(clientId))
                throw new RequestFailedApiException(RequestFailedApiException.ClientIdInvalid, "The client ID contains invalid characters");

            if (clientId.Length > MaxClientIdLength)
                throw new RequestFailedApiException(RequestFailedApiException.ClientIdTooLong, "The client ID is too long");

            return clientId;
        }

        public static string ProcessClientSecret(string clientSecret)
        {
            clientSecret = clientSecret?.Trim();

            if (string.IsNullOrEmpty(clientSecret))
                throw new RequestFailedApiException(RequestFailedApiException.ClientSecretMissing, "The client secret is missing");

            if (!ApiImpl.SafeString.IsMatch(clientSecret))
                throw new RequestFailedApiException(RequestFailedApiException.ClientSecretInvalid, "The client secret contains invalid characters");

            if (clientSecret.Length > MaxClientSecretLength)
                throw new RequestFailedApiException(RequestFailedApiException.ClientSecretTooLong, "The client secret is too long");

            return clientSecret;
        }

        public static string ProcessKey1(string key1, bool required)
        {
            key1 = key1?.Trim();

            if (string.IsNullOrEmpty(key1))
            {
                if (required)
                    throw new RequestFailedApiException(RequestFailedApiException.Key1Missing, "Key 1 is missing");

                return null;
            }

            if (!ApiImpl.SafeString.IsMatch(key1))
                throw new RequestFailedApiException(RequestFailedApiException.Key1Invalid, "Key 1 contains invalid characters");

            if (key1.Length > MaxKey1Length)
                throw new RequestFailedApiException(RequestFailedApiException.Key1TooLong, "Key 1 is too long");

            return key1;
        }

        public static string ProcessKey2(string key2, bool required)
        {
            key2 = key2.Trim();

            if (string.IsNullOrEmpty(key2))
            {
                if (required)
                    throw new RequestFailedApiException(RequestFailedApiException.Key2Missing, "Key 2 is missing");

                return null;
            }

            if (!ApiImpl.SafeString.IsMatch(key2))
                throw new RequestFailedApiException(RequestFailedApiException.Key2Invalid, "Key 2 contains invalid characters");

            if (key2.Length > MaxKey2Length)
                throw new RequestFailedApiException(RequestFailedApiException.Key2TooLong, "Key 2 is too long");

            return key2;
        }

        public static string ProcessKey3(string key3, bool required)
        {
            key3 = key3?.Trim();

            if (string.IsNullOrEmpty(key3))
            {
                if (required)
                    throw new RequestFailedApiException(RequestFailedApiException.Key3Missing, "Key 3 is missing");

                return null;
            }

            if (!ApiImpl.SafeString.IsMatch(key3))
                throw new RequestFailedApiException(RequestFailedApiException.Key3Invalid, "Key 3 contains invalid characters");

            if (key3.Length > MaxKey3Length)
                throw new RequestFailedApiException(RequestFailedApiException.Key3TooLong, "Key 3 is too long");

            return key3;
        }

        public static string ProcessKey4(string key4, bool required)
        {
            key4 = key4?.Trim();

            if (string.IsNullOrEmpty(key4))
            {
                if (required)
                    throw new RequestFailedApiException(RequestFailedApiException.Key4Missing, "Key 4 is missing");

                return null;
            }

            if (!ApiImpl.SafeString.IsMatch(key4))
                throw new RequestFailedApiException(RequestFailedApiException.Key4Invalid, "Key 4 contains invalid characters");

            if (key4.Length > MaxKey4Length)
                throw new RequestFailedApiException(RequestFailedApiException.Key4TooLong, "Key 4 is too long");

            return key4;
        }

        public static string ProcessRedirectUrlNotAllowed(string redirectUrl)
        {
            redirectUrl = redirectUrl?.Trim();

            if (!string.IsNullOrEmpty(redirectUrl))
                throw new RequestFailedApiException(RequestFailedApiException.RedirectUrlNotRequired, "The redirect URL is not required");

            return null;
        }

        public static string ProcessRedirectUrlRequired(string redirectUrl)
        {
            redirectUrl = redirectUrl?.Trim();

            if (string.IsNullOrEmpty(redirectUrl))
                throw new RequestFailedApiException(RequestFailedApiException.RedirectUrlMissing, "The redirect URL is missing");

            if (redirectUrl.Length > MaxRedirectUrlLength)
                throw new RequestFailedApiException(RequestFailedApiException.RedirectUrlTooLong, "The redirect URL is too long");

            try
            {
                Uri uri = new Uri(redirectUrl);
                if (!uri.IsAbsoluteUri)
                    throw new RequestFailedApiException(RequestFailedApiException.RedirectUrlMustBeAbsolute, "The redirect URL must be absolute");

                redirectUrl = uri.ToString();
            }
            catch (UriFormatException uriFormatException)
            {
                throw new RequestFailedApiException(RequestFailedApiException.RedirectUrlInvalid, $"The redirect URL is invalid: {uriFormatException.Message}");
            }

            return redirectUrl;
        }

        public static string ProcessCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new RequestFailedApiException(RequestFailedApiException.CodeMissing, "The code is missing");

            if (!SafeString.IsMatch(code))
                throw new RequestFailedApiException(RequestFailedApiException.CodeInvalid, "The code contains invalid characters");

            if (code.Length > MaxCodeLength)
                throw new RequestFailedApiException(RequestFailedApiException.CodeTooLong, "The code is too long");

            return code;
        }

        public static string ProcessName(string name, bool required = true)
        {
            name = name?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                if (required)
                    throw new RequestFailedApiException(RequestFailedApiException.NameMissing, "The name is missing");

                return null;
            }

            if (name.Length > MaxNameLength)
                throw new RequestFailedApiException(RequestFailedApiException.NameTooLong, "The name is too long");

            return name;
        }

        public static string ProcessShortDescription(string description)
        {
            description = description?.Trim();

            if (string.IsNullOrEmpty(description))
                return null;

            if (description.Length > MaxShortDescriptionLength)
                throw new RequestFailedApiException(RequestFailedApiException.DescriptionTooLong, "The description is too long");

            return description;
        }

        public static string ProcessLongDescription(string description)
        {
            description = description?.Trim();

            if (string.IsNullOrEmpty(description))
                return null;

            if (description.Length > MaxLongDescriptionLength)
                throw new RequestFailedApiException(RequestFailedApiException.DescriptionTooLong, "The description is too long");

            return description;
        }

        public static string ProcessComment(string comment)
        {
            comment = comment?.Trim();

            if (string.IsNullOrEmpty(comment))
                return null;

            if (comment.Length > MaxCommentLength)
                throw new RequestFailedApiException(RequestFailedApiException.CommentTooLong, "The comment is too long");

            return comment;
        }

        public static string ProcessOrderId(string orderId, bool required = true)
        {
            orderId = orderId?.Trim();

            if (string.IsNullOrEmpty(orderId))
            {
                if (required)
                    throw new RequestFailedApiException(RequestFailedApiException.OrderIdMissing, "The order ID is missing");

                return null;
            }

            if (orderId.Length > MaxOrderIdLength)
                throw new RequestFailedApiException(RequestFailedApiException.OrderIdTooLong, "The order ID is too long");

            return orderId.ToUpper();
        }

        public static string ProcessSubmissionMessage(string message)
        {
            message = message?.Trim();

            if (string.IsNullOrEmpty(message))
                return null;

            if (message.Length > MaxSubmissionMessageLength)
                throw new RequestFailedApiException(RequestFailedApiException.SubmissionMessageTooLong, "The submission message is too long");

            return message;
        }

        public static string ProcessRejectionReason(string reason)
        {
            reason = reason?.Trim();

            if (string.IsNullOrEmpty(reason))
                return null;

            if (reason.Length > MaxSubmissionMessageLength)
                throw new RequestFailedApiException(RequestFailedApiException.RejectionReasonTooLong, "The rejection reason is too long");

            return reason;
        }

        public static string ProcessConfirmationId(string confirmationId)
        {
            confirmationId = confirmationId?.Trim();

            if (string.IsNullOrEmpty(confirmationId))
                throw new RequestFailedApiException(RequestFailedApiException.ConfirmationIdMissing, "The confirmation ID is missing");

            if (confirmationId.Length > MaxConfirmationIdLength)
                throw new RequestFailedApiException(RequestFailedApiException.ConfirmationIdTooLong, "The confirmation ID is too long");

            return confirmationId.ToUpper();
        }

        public static string ProcessCancellationReason(string reason)
        {
            reason = reason?.Trim();

            if (string.IsNullOrEmpty(reason))
                throw new RequestFailedApiException(RequestFailedApiException.CancellationReasonMissing, "The cancellation reason is missing");

            if (reason.Length > MaxCancellationReasonLength)
                throw new RequestFailedApiException(RequestFailedApiException.CancellationReasonTooLong, "The cancellation reason is too long");

            return reason;
        }

        public static string ProcessExternalUserGroupIdentifier(string externalUserGroupIdentifier)
        {
            externalUserGroupIdentifier = externalUserGroupIdentifier?.Trim();

            if (string.IsNullOrEmpty(externalUserGroupIdentifier))
                return null;

            if (!SafeString.IsMatch(externalUserGroupIdentifier))
                throw new RequestFailedApiException(RequestFailedApiException.ExternalUserGroupIdentifierInvalid, "The external user group identifier contains invalid characters");

            if (externalUserGroupIdentifier.Length > MaxExternalUuidLength)
                throw new RequestFailedApiException(RequestFailedApiException.ExternalUserGroupIdentifierTooLong, "The external user group identifier is too long");

            return externalUserGroupIdentifier;
        }

        public static HashSet<string> ProcessExternalUserGroupIdentifiers(List<string> externalUserGroupIdentifiers)
        {
            if (externalUserGroupIdentifiers == null || externalUserGroupIdentifiers.Count == 0)
                return new HashSet<string>();

            return externalUserGroupIdentifiers.Select(externalUserGroupIdentifier => ProcessExternalUserGroupIdentifier(externalUserGroupIdentifier)).ToHashSet();
        }

        public static string ProcessSearchTerm(string searchTerm, bool required)
        {
            searchTerm = searchTerm?.Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                if (required)
                    throw new RequestFailedApiException(RequestFailedApiException.SearchTermMissing, "The search term is missing");

                return null;
            }

            if (searchTerm.Length > MaxSearchTermLength)
                throw new RequestFailedApiException(RequestFailedApiException.SearchTermTooLong, "The search term is too long");

            return searchTerm;
        }

        public static DateTimeOffset? ProcessLastUpdatedAtFrom(DateTimeOffset? lastUpdatedAtFrom)
        {
            return lastUpdatedAtFrom;
        }

        public static DateTimeOffset? ProcessLastUpdatedAtTo(DateTimeOffset? lastUpdatedAtFrom, DateTimeOffset? lastUpdatedAtTo)
        {
            if (lastUpdatedAtTo == null)
                return null;

            if (lastUpdatedAtFrom != null && lastUpdatedAtTo < lastUpdatedAtFrom)
                throw new RequestFailedApiException(RequestFailedApiException.LastUpdatedAtToBeforeLastUpdatedAtFrom, "The last updated at to date is set to a date before the last updated at date");

            return lastUpdatedAtTo;
        }

        public static bool ProcessApproved(bool? approved)
        {
            return approved ?? false;
        }

        public static bool ProcessDisabled(bool? disabled)
        {
            return disabled ?? false;
        }

        public static bool ProcessIsDefault(bool? isDefault)
        {
            return isDefault ?? true;
        }

        public static bool ProcessIsAdvanced(bool? isAdvanced)
        {
            return isAdvanced ?? false;
        }

        public static bool ProcessExcludeDisabledOrExpired(bool? excludeDisabledOrExpired)
        {
            return excludeDisabledOrExpired ?? true;
        }

        public static bool ProcessInvertSelection(bool? invertSelection)
        {
            return invertSelection ?? false;
        }

        public static bool ProcessEmailAddressConfirmed(bool? emailAddressConfirmed)
        {
            return emailAddressConfirmed ?? false;
        }

        public static string ProcessFirstName(string firstName)
        {
            firstName = firstName?.Trim();

            if (string.IsNullOrEmpty(firstName))
                return null;

            if (!SafeString.IsMatch(firstName))
                throw new RequestFailedApiException(RequestFailedApiException.FirstNameInvalid, $"The first name is invalid");

            if (firstName.Length > MaxFirstNameLength)
                throw new RequestFailedApiException(RequestFailedApiException.FirstNameTooLong, $"The first name address is too long");

            return firstName;
        }

        public static string ProcessLastName(string lastName)
        {
            lastName = lastName?.Trim();

            if (string.IsNullOrEmpty(lastName))
                return null;

            if (!SafeString.IsMatch(lastName))
                throw new RequestFailedApiException(RequestFailedApiException.LastNameInvalid, $"The last name is invalid");

            if (lastName.Length > MaxLastNameLength)
                throw new RequestFailedApiException(RequestFailedApiException.LastNameTooLong, $"The last name address is too long");

            return lastName;
        }

        public static string ProcessPhoneNumber(string phoneNumber)
        {
            phoneNumber = phoneNumber?.Trim();

            if (string.IsNullOrEmpty(phoneNumber))
                return null;

            if (!SafeString.IsMatch(phoneNumber))
                throw new RequestFailedApiException(RequestFailedApiException.PhoneNumberInvalid, $"The phone number is invalid");

            if (phoneNumber.Length > MaxPhoneNumberLength)
                throw new RequestFailedApiException(RequestFailedApiException.PhoneNumberTooLong, $"The phone number is too long");

            return phoneNumber;
        }

        public static bool ProcessPhoneNumberConfirmed(bool? phoneNumberConfirmed)
        {
            return phoneNumberConfirmed ?? false;
        }

        public static long ProcessNotificationUuid(string notificationUuid)
        {
            if (string.IsNullOrEmpty(notificationUuid))
                throw new RequestFailedApiException(RequestFailedApiException.NotificationUuidMissing, "The notification UUID is missing");

            try
            {
                return Convert.ToInt64(notificationUuid);
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.NotificationUuidInvalid, "The notification UUID is invalid");
            }
        }

        public static HashSet<long> ProcessNotificationUuids(List<string> notificationUuids)
        {
            if (notificationUuids == null ||
                notificationUuids.Count == 0)
            {
                return new HashSet<long>();
            }

            return notificationUuids.Select(notificationUuid => ProcessNotificationUuid(notificationUuid)).ToHashSet();
        }

        public static bool ProcessInvited(bool? invited)
        {
            return invited ?? false;
        }

        public static string ReplacePusherChannelNameInvalidCharacters(string channelName)
        {
            // see https://www.pusher.com/docs/channels/using_channels/channels

            return string.IsNullOrWhiteSpace(channelName) ?
                channelName : Regex.Replace(channelName, @"[^-=@,;_a-zA-Z0-9\.]", "_");
        }

        public static void ProcessBulkUpdateCount(int count)
        {
            if (count <= 0)
                throw new RequestFailedApiException(RequestFailedApiException.NoRecordsSpecified, "Please provide records for the update operation");

            if (count > MaxBulkUpdateCount)
                throw new RequestFailedApiException(RequestFailedApiException.TooManyUpdateRecords, $"Please only update up to {MaxBulkUpdateCount} records at once");
        }
    }
}
