using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace HCore.Database.ElasticSearch.Settings
{
    public class ExtendedElasticsearchClientSettings : ElasticsearchClientSettings
    {
        public ExtendedElasticsearchClientSettings(NodePool nodePool, SourceSerializerFactory sourceSerializer = null, SourceSerializerFactory requestResponseSerializer = null)
            : base(nodePool, sourceSerializer)
        {
            if (requestResponseSerializer != null)
            {
                UseThisRequestResponseSerializer = requestResponseSerializer(UseThisRequestResponseSerializer, this);
            }
        }
    }
}
