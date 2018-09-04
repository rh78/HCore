using ReinhardHolzner.HCore.ElasticSearch.Impl;

namespace ReinhardHolzner.HCore.ElasticSearch
{
    public interface IElasticSearchMappingInterface
    {
        string[] IndexNames { get; }

        void CreateIndex(IElasticSearchClient elasticSearchClient, string indexName);
        long UpdateIndex(IElasticSearchClient elasticSearchClient, string indexName, long indexVersion);
    }
}
