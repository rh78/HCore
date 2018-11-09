using Nest;

namespace HCore.Database.ElasticSearch
{
    public interface IElasticSearchDbContext
    {
        string[] IndexNames { get; }

        CreateIndexDescriptor GetCreateIndexDescriptor(IElasticSearchClient elasticSearchClient, string indexName);
        int GetIndexVersion(string indexName);
    }
}
