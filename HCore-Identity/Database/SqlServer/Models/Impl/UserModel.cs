using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HCore.Identity.Database.SqlServer.Models.Impl
{ 
    public class UserModel : IdentityUser
    {
        public const int MaxFirstNameLength = 50;
        public const int MaxLastNameLength = 50;
        public const int MaxUserUuidLength = 50;
        public const int MaxUserNameLength = 50;
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 50;

        [StringLength(MaxFirstNameLength)]
        public string FirstName { get; set; }

        [StringLength(MaxFirstNameLength)]
        public string LastName { get; set; }
    }
}
