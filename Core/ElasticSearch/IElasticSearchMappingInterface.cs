using Nest;

namespace ReinhardHolzner.HCore.ElasticSearch
{
    public interface IElasticSearchMappingInterface
    {
        string[] IndexNames { get; }

        CreateIndexDescriptor GetCreateIndexDescriptor(IElasticSearchClient elasticSearchClient, string indexName);
        long GetIndexVersion(string indexName);
    }
}
