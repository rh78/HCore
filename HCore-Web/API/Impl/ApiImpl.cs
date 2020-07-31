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

        public const int MaxExternalUuidLength = 100;
        public const int MaxEmailAddressLength = 50;
        public const int MaxAddressLineLength = 50;
        public const int MaxPostalCodeLength = 10;
        public const int MaxCityLength = 50;
        public const int MaxStateLength = 50;
        public const int MaxVatIdLength = 15;

        public const int MaxFirstNameLength = 50;
        public const int MaxLastNameLength = 50;

        public const int MaxContactPersonNameLength = MaxFirstNameLength + MaxLastNameLength + 1; // including space

        public const int MaxBulkUpdateCount = 50;

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

        public static void ProcessBulkUpdateCount(int count)
        {
            if (count <= 0)
                throw new RequestFailedApiException(RequestFailedApiException.NoRecordsSpecified, "Please provide records for the update operation");

            if (count > MaxBulkUpdateCount)
                throw new RequestFailedApiException(RequestFailedApiException.TooManyUpdateRecords, $"Please only update up to {MaxBulkUpdateCount} records at once");
        }
    }
}
