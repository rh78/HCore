/*
 * HCore Identity Auth API
 *
 * The HCore Identity Auth API provides the most common methods to handle authentication server side using ASP.NET Identity Core.
 *
 * OpenAPI spec version: 1.0.0-s2
 * Contact: holzner@invest-fit.at
 * Generated by: https://openapi-generator.tech
 */

using System;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using HCore.Identity.Resources;

namespace HCore.Identity.Models
{ 
    /// <summary>
    /// The information required to sign in the user
    /// </summary>
    [DataContract]
    [NotMapped]
	public partial class UserSignInSpec : IEquatable<UserSignInSpec>
    { 
		private string _Email;

        /// <summary>
        /// The email address of the user
        /// </summary>
        /// <value>The email address of the user</value>
        [Required(ErrorMessageResourceType = typeof(Translations.Resources.Messages), ErrorMessageResourceName = "email_missing")]
        [Display(ResourceType = typeof(Messages), Name = "email_address")]
        [DataMember(Name = "email")]
        [DataType(DataType.EmailAddress, ErrorMessageResourceType = typeof(Translations.Resources.Messages), ErrorMessageResourceName = "email_invalid")]
        public string Email { get => _Email; set { _Email = value; EmailSet = true; } }
		
		public bool EmailSet = false;		

		private string _Password;

        /// <summary>
        /// The password of the user
        /// </summary>
        /// <value>The password of the user</value>
        [Required(ErrorMessageResourceType = typeof(Translations.Resources.Messages), ErrorMessageResourceName = "password_missing")]
        [Display(ResourceType = typeof(Messages), Name = "password")]
        [DataMember(Name = "password")]
        public string Password { get => _Password; set { _Password = value; PasswordSet = true; } }
		
		public bool PasswordSet = false;		

		private bool? _Remember;

        /// <summary>
        /// Flag to indicate if the user sign in should be remembered or not
        /// </summary>
        /// <value>Flag to indicate if the user sign in should be remembered or not</value>
        [Display(ResourceType = typeof(Messages), Name = "remember")]
        [DataMember(Name="remember")]
		public bool Remember { get => _Remember != null ? (bool)_Remember : false; set { _Remember = value; RememberSet = true; } }
		
		public bool RememberSet = false;	
        
        public string SegmentAnonymousUserUuid { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class UserSignInSpec {\n");
            sb.Append("  Email: ").Append(Email).Append("\n");
            sb.Append("  Password: ").Append(Password).Append("\n");
            sb.Append("  Remember: ").Append(Remember).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((UserSignInSpec)obj);
        }

        /// <summary>
        /// Returns true if UserSignInSpec instances are equal
        /// </summary>
        /// <param name="other">Instance of UserSignInSpec to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(UserSignInSpec other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return 
                (
                    Email == other.Email ||
                    Email != null &&
                    Email.Equals(other.Email)
                ) && 
                (
                    Password == other.Password ||
                    Password != null &&
                    Password.Equals(other.Password)
                ) && 
                (
                    Remember == other.Remember &&
                    Remember.Equals(other.Remember)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                // Suitable nullity checks etc, of course :)
                    if (Email != null)
                    hashCode = hashCode * 59 + Email.GetHashCode();
                    if (Password != null)
                    hashCode = hashCode * 59 + Password.GetHashCode();                    
                    hashCode = hashCode * 59 + Remember.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
        #pragma warning disable 1591

        public static bool operator ==(UserSignInSpec left, UserSignInSpec right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(UserSignInSpec left, UserSignInSpec right)
        {
            return !Equals(left, right);
        }

        #pragma warning restore 1591
        #endregion Operators
    }
}
