using System.ComponentModel.DataAnnotations;

namespace HCore.Identity.Database.SqlServer.Models.Impl
{
    public class WhitelistEmailAddressModel
    {
        [Key]
        public long Uuid { get; set; }

        public string NormalizedEmailAddress { get; set; }
    }
}
