using System;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest;

namespace HCore.Database.Extensions
{
    public static class IElasticClientExtensions
    {
        public static async Task<bool?> IsAvailableAsync(this IElasticClient elasticClient, CancellationToken cancellationToken = default)
        {
            if (elasticClient == null)
            {
                throw new ArgumentNullException(nameof(elasticClient));
            }

            try
            {
                var elasticSearchPingResponse = await elasticClient.PingAsync(ct: cancellationToken).ConfigureAwait(false);

                return elasticSearchPingResponse.IsValid;
            }
            catch (ElasticsearchClientException ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    return null;
                }

                return false;
            }
        }
    }
}
