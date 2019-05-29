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
        public static readonly Regex Uuid = new Regex(@"^[a-zA-Z0-9\._\-\:]+$");
        public static readonly Regex SafeString = new Regex(@"^[\w\s\.@_\-\+\=:/]+$");

        public static readonly CultureInfo DefaultCultureInfo = CultureInfo.GetCultureInfo("en-US");

        public const int MaxExternalUuidLength = 100;
        public const int MaxEmailAddressLength = 50;

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

        public static void ProcessBulkUpdateCount(int count)
        {
            if (count <= 0)
                throw new RequestFailedApiException(RequestFailedApiException.NoRecordsSpecified, "Please provide records for the update operation");

            if (count > MaxBulkUpdateCount)
                throw new RequestFailedApiException(RequestFailedApiException.TooManyUpdateRecords, $"Please only update up to {MaxBulkUpdateCount} records at once");
        }
    }
}
