using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace HCore.Identity.Database.SqlServer.Models.Impl
{ 
    public class UserModel : IdentityUser
    {
        public static readonly Regex ScopedEmail = new Regex(@"^[0-9]+:[0-9]+:.+$");

        public const int MaxUserUuidLength = 100;
        public const int MaxUserNameLength = 50;
        public const int MaxEmailAddressLength = 50;

        public const int MaxFirstNameLength = 50;
        public const int MaxLastNameLength = 50;
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 50;

        [StringLength(MaxFirstNameLength)]
        public string FirstName { get; set; }

        [StringLength(MaxFirstNameLength)]
        public string LastName { get; set; }

        public List<string> MemberOf { get; set; }

        public string NotificationCulture { get; set; }
        
        public bool GroupNotifications { get; set; }

        public string Currency { get; set; }

        public DateTimeOffset? PrivacyPolicyAccepted { get; set; }
        public string PrivacyPolicyUrl { get; set; }
        public int? PrivacyPolicyVersionAccepted { get; set; }

        public DateTimeOffset? TermsAndConditionsAccepted { get; set; }
        public string TermsAndConditionsUrl { get; set; }
        public int? TermsAndConditionsVersionAccepted { get; set; }

        public DateTimeOffset? CommunicationAccepted { get; set; }
        public string CommunicationUrl { get; set; }
        public int? CommunicationVersionAccepted { get; set; }

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                return $"{FirstName} {LastName}";
            
            return GetEmail();
        }

        public string GetEmail()
        {
            if (string.IsNullOrEmpty(Email))
                return Email;

            if (ScopedEmail.IsMatch(Email))
            {
                // we have scoped email prefix

                string[] emailParts = Email.Split(":");

                string unscopedEmail = string.Join(":", emailParts.Skip(2));

                if (string.IsNullOrEmpty(unscopedEmail))
                    return null;

                return unscopedEmail;
            }

            return Email;
        }

        public long? GetScopedTenantUuid()
        {
            if (string.IsNullOrEmpty(Email))
                return null;

            if (ScopedEmail.IsMatch(Email))
            {
                // we have scoped email prefix

                string[] emailParts = Email.Split(":");

                string scopedTenantUuidString = emailParts[1];

                try
                {
                    return Convert.ToInt64(scopedTenantUuidString);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }
    }
}
