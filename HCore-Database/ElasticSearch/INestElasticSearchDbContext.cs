using Nest;

namespace HCore.Database.ElasticSearch
{
    public interface INestElasticSearchDbContext
    {
        string[] IndexNames { get; }

        CreateIndexDescriptor GetCreateIndexDescriptor(INestElasticSearchClient elasticSearchClient, string indexName);
        int GetIndexVersion(string indexName);
    }
}
