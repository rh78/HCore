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

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                return $"{LastName} {FirstName}";
            
            return Email;
        }
    }
}
