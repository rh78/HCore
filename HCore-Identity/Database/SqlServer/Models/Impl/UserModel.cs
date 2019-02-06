using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace HCore.Identity.Database.SqlServer.Models.Impl
{ 
    public class UserModel : IdentityUser
    {
        public const int MaxUserUuidLength = 50;
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
            
            return Email;
        }
    }
}
