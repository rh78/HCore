using Nest;

namespace ReinhardHolzner.HCore.Database.ElasticSearch
{
    public interface IElasticSearchDbContext
    {
        string[] IndexNames { get; }

        CreateIndexDescriptor GetCreateIndexDescriptor(IElasticSearchClient elasticSearchClient, string indexName);
        long GetIndexVersion(string indexName);
    }
}
