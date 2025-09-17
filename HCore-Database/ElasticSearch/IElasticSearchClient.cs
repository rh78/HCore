using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;

namespace HCore.Database.ElasticSearch
{
    public interface IElasticSearchClient
    {
        ElasticsearchClient ElasticsearchClient { get; }

        void Initialize();

        void ConfigureNonConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting);
        void ConfigureConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting);
    }
}
