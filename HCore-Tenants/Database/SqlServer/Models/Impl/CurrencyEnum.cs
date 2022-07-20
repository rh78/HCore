using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    /// <summary>
    /// Currently supported currencies
    /// </summary>
    /// <value>Currently supported currencies</value>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum CurrencyEnum
    {
        /// <summary>
        /// Enum UsdEnum for usd
        /// </summary>
        [EnumMember(Value = "usd")]
        UsdEnum = 1 - 1,

        /// <summary>
        /// Enum EurEnum for eur
        /// </summary>
        [EnumMember(Value = "eur")]
        EurEnum = 2 - 1
    }
}
