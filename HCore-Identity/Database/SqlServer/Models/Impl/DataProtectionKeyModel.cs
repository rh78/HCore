// Copyright (c) 2018 KevDever. All rights reserved.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

// Original at https://github.com/kevdever/SqlServerDataProtectionProvider

// Adjusted for HCore by Reinhard Holzner

using System.ComponentModel.DataAnnotations;

namespace HCore.Identity.Database.SqlServer.Models.Impl
{
    /// <summary>
    /// An object to hold a key that is persisted to the database.
    /// </summary>
    public class DataProtectionKeyModel
    {
        [Key]
        [StringLength(449)]
        public string FriendlyName { get; set; }

        // This property is stored as a string in the database. I found that storing the XML as XML in the database resulted in formatting problems that DPAPI did not like.
        // If you wish to try with a SQL XML datatype anyway, update the below annotation with the following: [Required, Column(TypeName = "xml")]
        [Required]       
        public string XmlData { get; set; }
    }
}
