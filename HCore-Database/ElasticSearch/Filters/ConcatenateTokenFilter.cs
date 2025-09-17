using Elastic.Clients.Elasticsearch.Analysis;

namespace HCore.Database.ElasticSearch.Filters
{
    public class ConcatenateTokenFilter : ITokenFilter
    {
        public string Type => "concatenate";

        public string TokenSeparator { get; set; }

        public int? IncrementGap { get; set; }
    }
}
