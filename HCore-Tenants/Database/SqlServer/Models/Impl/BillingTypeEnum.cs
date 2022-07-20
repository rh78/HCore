using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    /// <summary>
    /// Gets or Sets BillingType
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum SubscriptionBillingTypeEnum
    {

        /// <summary>
        /// Enum MonthlyInvoiceEnum for monthly_invoice
        /// </summary>
        [EnumMember(Value = "monthly_invoice")]
        MonthlyInvoiceEnum = 1 - 1,

        /// <summary>
        /// Enum YearlyInvoiceEnum for yearly_invoice
        /// </summary>
        [EnumMember(Value = "yearly_invoice")]
        YearlyInvoiceEnum = 2 - 1,

        /// <summary>
        /// Enum MonthlyAutomaticEnum for monthly_automatic
        /// </summary>
        [EnumMember(Value = "monthly_automatic")]
        MonthlyAutomaticEnum = 3 - 1,

        /// <summary>
        /// Enum YearlyAutomaticEnum for yearly_automatic
        /// </summary>
        [EnumMember(Value = "yearly_automatic")]
        YearlyAutomaticEnum = 4 - 1
    }
}
