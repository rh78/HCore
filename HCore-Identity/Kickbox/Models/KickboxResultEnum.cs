using System.Runtime.Serialization;

namespace HCore.Identity.Kickbox.Models
{
    public enum KickboxResultEnum
    {
        [EnumMember(Value = "deliverable")]
        Deliverable,

        [EnumMember(Value = "undeliverable")]
        Undeliverable,

        [EnumMember(Value = "risky")]
        Risky,

        [EnumMember(Value = "unknown")]
        Unknown
    }
}
