using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport;
using Elastic.Transport.Extensions;
using HCore.Database.ElasticSearch.JsonConverters;
using HCore.Database.ElasticSearch.Models;
using HCore.Database.ElasticSearch.Serializers;
using HCore.Database.ElasticSearch.Settings;

namespace HCore.Database.ElasticSearch.Impl
{
    internal class ElasticSearchClientImpl : IElasticSearchClient
    {
        private const string IndexVersionsIndexName = "indexversions";

        private readonly bool _isProduction;

        private readonly int _numberOfShards;
        private readonly int _numberOfReplicas;
        private readonly string _hosts;

        public ElasticsearchClient ElasticsearchClient { get; private set; }

        private readonly IElasticSearchDbContext _elasticSearchDbContext;

        private readonly bool _useJsonNetSerializer;

        public ElasticSearchClientImpl(bool isProduction, int numberOfShards, int numberOfReplicas, string hosts, IElasticSearchDbContext elasticSearchDbContext, bool useJsonNetSerializer)
        {
            _isProduction = isProduction;

            _numberOfShards = numberOfShards;
            _numberOfReplicas = numberOfReplicas;
            _hosts = hosts;

            _elasticSearchDbContext = elasticSearchDbContext;

            _useJsonNetSerializer = useJsonNetSerializer;
        }

        public void Initialize()
        {
            string[] hosts = _hosts.Split(',');
            if (hosts.Length == 0)
                throw new Exception("ElasticSearch hosts list is empty");

            List<Uri> uriList = new List<Uri>();

            hosts.ToList().ForEach(originalHost =>
            {
                string host = originalHost;
                int port = 9200;

                string[] splittedHost = host.Split(':');
                if (splittedHost.Length > 1)
                {
                    host = splittedHost[0];
                    if (string.IsNullOrEmpty(host))
                        throw new Exception("ElasticSearch host is invalid");

                    port = Convert.ToInt32(splittedHost[1]);

                    if (port < 1)
                        throw new Exception("ElasticSearch host port is invalid");
                }

                Uri uri = new Uri($"http://{host}:{port}");
                uriList.Add(uri);
            });

            NodePool nodePool =
                uriList.Count > 1 ?
                    new SniffingNodePool(uriList) :
                    new SingleNodePool(uriList[0]);

            ElasticsearchClientSettings settings;

            if (_useJsonNetSerializer)
            {
                settings = new ExtendedElasticsearchClientSettings(
                    nodePool,
                    sourceSerializer: (serializer, settings) => new NewtonsoftSourceSerializer(serializer, settings),
                    requestResponseSerializer: (serializer, settings) =>
                    {
                        serializer.TryGetJsonSerializerOptions(out var options);

                        options.Converters.Add(new ConcatenateTokenFilterConverter());

                        return serializer;
                    })
                    .DisableAutomaticProxyDetection()
                    .ThrowExceptions();
            }
            else
            {
                settings = new ExtendedElasticsearchClientSettings(
                    nodePool,
                     requestResponseSerializer: (serializer, settings) =>
                     {
                         serializer.TryGetJsonSerializerOptions(out var options);

                         options.Converters.Add(new ConcatenateTokenFilterConverter());

                         return serializer;
                     })
                    .DisableAutomaticProxyDetection()
                    .ThrowExceptions();
            }

            ElasticsearchClient = new ElasticsearchClient(settings);

            CreateIndexVersionsIndex();

            string[] indexNames = _elasticSearchDbContext.IndexNames;

            indexNames.ToList().ForEach(indexName =>
            {
                int newestIndexVersion = _elasticSearchDbContext.GetIndexVersion(indexName);

                IndexVersion indexVersion = GetIndexVersion(indexName);

                if (indexVersion.Version < 1)
                {
                    CreateIndexVersion(indexName, 0, newestIndexVersion);
                }
                else
                {
                    if (newestIndexVersion > indexVersion.Version)
                        CreateIndexVersion(indexName, indexVersion.Version, newestIndexVersion);
                }
            });
        }

        private void CreateIndexVersionsIndex()
        {
            var indexVersionsIndexExists = ElasticsearchClient.Indices.Exists(IndexVersionsIndexName).Exists;

            if (!indexVersionsIndexExists)
            {
                Console.WriteLine("Creating index versions index...");

                var createIndexResponse = ElasticsearchClient.Indices.Create(IndexVersionsIndexName, indexVersionsIndex => indexVersionsIndex
                    .Mappings(indexVersion => indexVersion
                        .Properties<IndexVersion>(indexVersionProperty => indexVersionProperty
                            .Keyword(element => element.Name)
                            .LongNumber(element => element.Version)
                        )
                        .Dynamic(DynamicMapping.False)
                    )
                    .Settings(indexVersionSetting => ConfigureNonConcatenateAndAutocompleteSettings(indexVersionSetting))
                );

                if (!createIndexResponse.Acknowledged)
                {
                    throw new Exception($"Cannot create index versions index: {createIndexResponse.ElasticsearchServerError}");
                }

                Console.WriteLine("Index versions index created");
            }
        }

        private void CreateIndexVersion(string indexName, long oldVersion, int newVersion)
        {
            string oldIndexNameWithVersion = indexName + "_v" + oldVersion;
            string newIndexNameWithVersion = indexName + "_v" + newVersion;

            Console.WriteLine($"Creating index {newIndexNameWithVersion}...");

            var createIndexResponse = ElasticsearchClient.Indices.Create(newIndexNameWithVersion, createIndexRequestDescriptor => _elasticSearchDbContext.CreateIndexRequestDescriptor(this, createIndexRequestDescriptor, indexName));

            Console.WriteLine($"Index {newIndexNameWithVersion} created");

            if (oldVersion > 0)
            {
                // we have an old index, reindex

                Console.WriteLine($"Reindexing from {oldIndexNameWithVersion} to {newIndexNameWithVersion}...");

                var reindexOnServerResult = ElasticsearchClient.Reindex(reindex => reindex
                    .Source(source => source
                        .Indices(oldIndexNameWithVersion)
                    )
                    .Dest(destinationDescriptor => destinationDescriptor
                        .Index(newIndexNameWithVersion)
                        .VersionType(VersionType.Internal)
                    ));

                Console.WriteLine($"Reindexed from {oldIndexNameWithVersion} to {newIndexNameWithVersion} " +
                    $"({reindexOnServerResult.Created} created, {reindexOnServerResult.Updated} updated)");
            }

            var aliasExistsResponse = ElasticsearchClient.Indices.ExistsAlias(indexName);

            if (aliasExistsResponse.Exists)
            {
                // we need to remove the old alias

                Console.WriteLine($"Deleting alias {indexName} -> {oldIndexNameWithVersion}...");

                ElasticsearchClient.Indices.DeleteAlias(oldIndexNameWithVersion, indexName);

                Console.WriteLine($"Deleted alias {indexName} -> {oldIndexNameWithVersion}");
            }

            // we need to add the new alias

            Console.WriteLine($"Creating alias {indexName} -> {newIndexNameWithVersion}...");

            var createAliasResponse = ElasticsearchClient.Indices.PutAlias(indices: newIndexNameWithVersion, name: indexName);

            if (!createAliasResponse.Acknowledged)
                throw new Exception($"Cannot create alias {indexName} -> {newIndexNameWithVersion}");

            Console.WriteLine($"Created alias {indexName} -> {newIndexNameWithVersion}");

            var newIndexVersion = new IndexVersion()
            {
                Name = indexName,
                Version = newVersion
            };

            UpdateIndexVersion(newIndexVersion);

            if (oldVersion > 0)
            {
                if (!_isProduction)
                {
                    // if everything succeeded, remove the old index

                    // do not delete old index data in production because it is just too dangerous

                    Console.WriteLine($"Deleting old index {oldIndexNameWithVersion}");

                    var deleteIndexResponse = ElasticsearchClient.Indices.Delete(oldIndexNameWithVersion);

                    if (!deleteIndexResponse.Acknowledged)
                        throw new Exception($"Cannot delete old index {oldIndexNameWithVersion}");

                    Console.WriteLine($"Deleted old index {oldIndexNameWithVersion}");
                }
                else
                {
                    Console.WriteLine($"WARNING: do not forget to delete old index {oldIndexNameWithVersion} if everything is all right!");
                }
            }
        }

        public void ConfigureNonConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting)
        {
            setting
                .NumberOfShards(_numberOfShards)
                .NumberOfReplicas(_numberOfReplicas)
                .AddOtherSetting("index.max_ngram_diff", int.MaxValue)
                .AddOtherSetting("index.gc_deletes", "1h"); // 1 hour
        }

        public void ConfigureConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting)
        {
            setting
                .NumberOfShards(_numberOfShards)
                .NumberOfReplicas(_numberOfReplicas)
                .Analysis(analysis => ConfigureConcatenateAndAutocompleteAnalysis(analysis))
                .AddOtherSetting("index.max_ngram_diff", int.MaxValue)
                .AddOtherSetting("index.gc_deletes", "1h"); // 1 hour
        }

        private void ConfigureConcatenateAndAutocompleteAnalysis(IndexSettingsAnalysisDescriptor analysis)
        {
            // for concatenate filter see my fork: https://github.com/rh78/elasticsearch-concatenate-token-filter

            var tokenFilters = new TokenFilters
            {
                {
                    "concatenate_filter",
                    new Filters.ConcatenateTokenFilter()
                    {
                        TokenSeparator = " ",
                        IncrementGap = 1000
                    }
                },
                {
                    "edge_ngram_filter",
                    new EdgeNGramTokenFilter()
                    {
                        MinGram = 1,
                        MaxGram = 50
                    }
                },
                {
                    "ngram_filter",
                    new NGramTokenFilter()
                    {
                        MinGram = 3,
                        MaxGram = 50
                    }
                },
                {
                    "short_ngram_filter",
                    new NGramTokenFilter()
                    {
                        MinGram = 1,
                        MaxGram = 50
                    }
                }
            };

            analysis
                .TokenFilters(tokenFilters)
                .Analyzers(analyzer => analyzer
                    .Custom("edge_ngram_concatenate_index", custom => custom
                        .Tokenizer("standard")
                        .Filter(new string[] { "lowercase", "asciifolding", "concatenate_filter", "edge_ngram_filter" })
                    )
                    .Custom("edge_ngram_partial_index", custom => custom
                        .Tokenizer("standard")
                        .Filter(new string[] { "lowercase", "asciifolding", "edge_ngram_filter" })
                    )
                    .Custom("ngram_concatenate_index", custom => custom
                        .Tokenizer("standard")
                        .Filter(new string[] { "lowercase", "asciifolding", "concatenate_filter", "ngram_filter" })
                    )
                    .Custom("short_ngram_concatenate_index", custom => custom
                        .Tokenizer("standard")
                        .Filter(new string[] { "lowercase", "asciifolding", "concatenate_filter", "short_ngram_filter" })
                    )
                    .Custom("ngram_partial_index", custom => custom
                        .Tokenizer("standard")
                        .Filter(new string[] { "lowercase", "asciifolding", "ngram_filter" })
                    )
                    .Custom("short_ngram_partial_index", custom => custom
                        .Tokenizer("standard")
                        .Filter(new string[] { "lowercase", "asciifolding", "short_ngram_filter" })
                    )
                    .Custom("concatenate_search", custom => custom
                        .Tokenizer("standard")
                        .Filter(new string[] { "lowercase", "asciifolding", "concatenate_filter" })
                    )
                    .Custom("partial_search", custom => custom
                        .Tokenizer("standard")
                        .Filter(new string[] { "lowercase", "asciifolding" })
                    )
                );
        }

        private IndexVersion GetIndexVersion(string indexName)
        {
            bool indexVersionExists = ElasticsearchClient.Exists<IndexVersion>(indexName, get => get
                .Index(IndexVersionsIndexName)).Exists;

            if (!indexVersionExists)
            {
                var newIndexVersion = new IndexVersion()
                {
                    Name = indexName,
                    Version = 0
                };

                UpdateIndexVersion(newIndexVersion);

                return newIndexVersion;
            }
            else
            {
                var getIndexVersionResponse = ElasticsearchClient.Get<IndexVersion>(indexName, get => get
                    .Index(IndexVersionsIndexName));

                return getIndexVersionResponse.Source;
            }
        }

        private void UpdateIndexVersion(IndexVersion indexVersion)
        {
            ElasticsearchClient.Index(indexVersion, index => index
                .Index(IndexVersionsIndexName)
                .Id(indexVersion.Name));
        }
    }
}
