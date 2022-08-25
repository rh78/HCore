using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;

namespace HCore.Identity.Database.SqlServer.Models.Impl
{ 
    public class UserModel : IdentityUser
    {
        public static readonly Regex ScopedEmail3Parts = new Regex(@"^[0-9]+:[0-9]+:.+$");
        public static readonly Regex ScopedEmail4Parts = new Regex(@"^[0-9]+:[0-9]+:[0-9]+:.+$");

        [StringLength(Web.API.Impl.ApiImpl.MaxFirstNameLength)]
        public string FirstName { get; set; }

        [StringLength(Web.API.Impl.ApiImpl.MaxFirstNameLength)]
        public string LastName { get; set; }

        [StringLength(Web.API.Impl.ApiImpl.MaxOrganizationLength)]
        public string Organization { get; set; }

        [StringLength(Web.API.Impl.ApiImpl.MaxCustomIdentifierLength)]
        public string CustomIdentifier { get; set; }

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

        public string NormalizedEmailWithoutScope { get; set; }

        public long? DeveloperUuid { get; set; }
        public long? TenantUuid { get; set; }

        public long? AuthScopeConfigurationUuid { get; set; }

        [Column(TypeName="text")]
        public string ClaimsJson { get; set; }

        public string AccessTokenCache { get; set; }
        public string IdentityTokenCache { get; set; }
        public string RefreshTokenCache { get; set; }

        public DateTimeOffset? ExpiryDate { get; set; }

        [MaxLength(Web.API.Impl.ApiImpl.MaxExternalUuidLength)]
        public string ExternalUuid { get; set; }

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

            // we have scoped email prefix

            if (AuthScopeConfigurationUuid == null)
            {
                if (ScopedEmail3Parts.IsMatch(Email))
                {
                    string[] emailParts = Email.Split(":");

                    string unscopedEmail = string.Join(":", emailParts.Skip(2));

                    if (string.IsNullOrEmpty(unscopedEmail))
                        return null;

                    return unscopedEmail;
                }
            }
            else
            {
                if (ScopedEmail4Parts.IsMatch(Email))
                {
                    string[] emailParts = Email.Split(":");

                    string unscopedEmail = string.Join(":", emailParts.Skip(3));

                    if (string.IsNullOrEmpty(unscopedEmail))
                        return null;

                    return unscopedEmail;
                }
            }

            return Email;
        }

        public long? GetScopedTenantUuid()
        {
            if (string.IsNullOrEmpty(Email))
                return null;

            if (AuthScopeConfigurationUuid == null)
            {
                if (ScopedEmail3Parts.IsMatch(Email))
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
            }
            else
            {
                if (ScopedEmail4Parts.IsMatch(Email))
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
            }

            return null;
        }
    }
}
