using Elastic.Clients.Elasticsearch.IndexManagement;

namespace HCore.Database.ElasticSearch
{
    public interface IElasticSearchDbContext
    {
        string[] IndexNames { get; }

        void CreateIndexRequestDescriptor(IElasticSearchClient elasticSearchClient, CreateIndexRequestDescriptor createIndexRequestDescriptor, string indexName);

        int GetIndexVersion(string indexName);
    }
}
