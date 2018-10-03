/*
 * RHCore Identity Auth API
 *
 * The RHCore Identity Auth API provides the most common methods to handle authentication server side using ASP.NET Identity Core.
 *
 * OpenAPI spec version: 1.0.0-s2
 * Contact: holzner@invest-fit.at
 * Generated by: https://openapi-generator.tech
 */

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ReinhardHolzner.Core.Identity.Generated.Models
{ 
    /// <summary>
    /// Information about an user
    /// </summary>
    [DataContract]
    [NotMapped]
	public partial class User : IEquatable<User>
    { 
		private string _Uuid;
		
		/// <summary>
        /// The UUID of the user
        /// </summary>
        /// <value>The UUID of the user</value>
        [DataMember(Name="uuid")]
		public string Uuid { get => _Uuid; set { _Uuid = value; UuidSet = true; } }
		
		public bool UuidSet = false;		

		private string _Email;
		
		/// <summary>
        /// The email address of the user
        /// </summary>
        /// <value>The email address of the user</value>
        [DataMember(Name="email")]
		public string Email { get => _Email; set { _Email = value; EmailSet = true; } }
		
		public bool EmailSet = false;		

		private bool? _EmailConfirmed;
		
		/// <summary>
        /// Indicates if the email address of the user has already been confirmed
        /// </summary>
        /// <value>Indicates if the email address of the user has already been confirmed</value>
        [DataMember(Name="email_confirmed")]
		public bool? EmailConfirmed { get => _EmailConfirmed; set { _EmailConfirmed = value; EmailConfirmedSet = true; } }
		
		public bool EmailConfirmedSet = false;		

		private string _PhoneNumber;
		
		/// <summary>
        /// The phone number of the user
        /// </summary>
        /// <value>The phone number of the user</value>
        [DataMember(Name="phone_number")]
		public string PhoneNumber { get => _PhoneNumber; set { _PhoneNumber = value; PhoneNumberSet = true; } }
		
		public bool PhoneNumberSet = false;		

		private bool? _PhoneNumberConfirmed;
		
		/// <summary>
        /// Indicates if the phone number of the user has already been confirmed
        /// </summary>
        /// <value>Indicates if the phone number of the user has already been confirmed</value>
        [DataMember(Name="phone_number_confirmed")]
		public bool? PhoneNumberConfirmed { get => _PhoneNumberConfirmed; set { _PhoneNumberConfirmed = value; PhoneNumberConfirmedSet = true; } }
		
		public bool PhoneNumberConfirmedSet = false;		

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class User {\n");
            sb.Append("  Uuid: ").Append(Uuid).Append("\n");
            sb.Append("  Email: ").Append(Email).Append("\n");
            sb.Append("  EmailConfirmed: ").Append(EmailConfirmed).Append("\n");
            sb.Append("  PhoneNumber: ").Append(PhoneNumber).Append("\n");
            sb.Append("  PhoneNumberConfirmed: ").Append(PhoneNumberConfirmed).Append("\n");
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
            return obj.GetType() == GetType() && Equals((User)obj);
        }

        /// <summary>
        /// Returns true if User instances are equal
        /// </summary>
        /// <param name="other">Instance of User to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(User other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return 
                (
                    Uuid == other.Uuid ||
                    Uuid != null &&
                    Uuid.Equals(other.Uuid)
                ) && 
                (
                    Email == other.Email ||
                    Email != null &&
                    Email.Equals(other.Email)
                ) && 
                (
                    EmailConfirmed == other.EmailConfirmed ||
                    EmailConfirmed != null &&
                    EmailConfirmed.Equals(other.EmailConfirmed)
                ) && 
                (
                    PhoneNumber == other.PhoneNumber ||
                    PhoneNumber != null &&
                    PhoneNumber.Equals(other.PhoneNumber)
                ) && 
                (
                    PhoneNumberConfirmed == other.PhoneNumberConfirmed ||
                    PhoneNumberConfirmed != null &&
                    PhoneNumberConfirmed.Equals(other.PhoneNumberConfirmed)
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
                    if (Uuid != null)
                    hashCode = hashCode * 59 + Uuid.GetHashCode();
                    if (Email != null)
                    hashCode = hashCode * 59 + Email.GetHashCode();
                    if (EmailConfirmed != null)
                    hashCode = hashCode * 59 + EmailConfirmed.GetHashCode();
                    if (PhoneNumber != null)
                    hashCode = hashCode * 59 + PhoneNumber.GetHashCode();
                    if (PhoneNumberConfirmed != null)
                    hashCode = hashCode * 59 + PhoneNumberConfirmed.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
        #pragma warning disable 1591

        public static bool operator ==(User left, User right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(User left, User right)
        {
            return !Equals(left, right);
        }

        #pragma warning restore 1591
        #endregion Operators
    }
}