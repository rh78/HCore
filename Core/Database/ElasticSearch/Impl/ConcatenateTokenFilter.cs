using Nest;
using Newtonsoft.Json;

namespace ReinhardHolzner.Core.Database.ElasticSearch.Impl
{
    public interface IConcatenateTokenFilter : ITokenFilter
    {
        [JsonProperty("token_separator")]
        string TokenSeparator { get; set; }

        [JsonProperty("increment_gap")]
        int? IncrementGap { get; set; }
    }

    public class ConcatenateTokenFilter : TokenFilterBase, IConcatenateTokenFilter
    {
        public ConcatenateTokenFilter() : base("concatenate") { }

        public string TokenSeparator { get; set; }

        public int? IncrementGap { get; set; }
    }

    public class ConcatenateTokenFilterDescriptor
        : TokenFilterDescriptorBase<ConcatenateTokenFilterDescriptor, IConcatenateTokenFilter>, IConcatenateTokenFilter
    {
        protected override string Type => "concatenate";

        string IConcatenateTokenFilter.TokenSeparator { get; set; }
        int? IConcatenateTokenFilter.IncrementGap { get; set; }

        public ConcatenateTokenFilterDescriptor TokenSeparator(string tokenSeparator) => Assign(a => a.TokenSeparator = tokenSeparator);

        public ConcatenateTokenFilterDescriptor IncrementGap(int? incrementGap) => Assign(a => a.IncrementGap = incrementGap);
    }
}
