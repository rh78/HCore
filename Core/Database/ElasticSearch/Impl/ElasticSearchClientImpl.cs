using Elasticsearch.Net;
using Nest;
using ReinhardHolzner.Core.Database.ElasticSearch.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReinhardHolzner.Core.Database.ElasticSearch.Impl
{
    internal class ElasticSearchClientImpl : IElasticSearchClient
    {
        private const string IndexVersionsIndexName = "indexversions";

        private bool _isProduction;

        private int _numberOfShards;
        private int _numberOfReplicas;
        private string _hosts;        

        public ElasticClient ElasticClient { get; private set; }

        private IElasticSearchDbContext _elasticSearchDbContext;

        public ElasticSearchClientImpl(bool isProduction, int numberOfShards, int numberOfReplicas, string hosts, IElasticSearchDbContext elasticSearchDbContext)
        {
            _isProduction = isProduction;

            _numberOfShards = numberOfShards;
            _numberOfReplicas = numberOfReplicas;
            _hosts = hosts;

            _elasticSearchDbContext = elasticSearchDbContext;
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

            IConnectionPool connectionPool = 
                uriList.Count > 1 ? 
                    (IConnectionPool) new SniffingConnectionPool(uriList) :
                    (IConnectionPool) new SingleNodeConnectionPool(uriList[0]);

            var settings = new ConnectionSettings(connectionPool)
                .DisableAutomaticProxyDetection()
                .ThrowExceptions();

            ElasticClient = new ElasticClient(settings);

            CreateIndexVersionsIndex();

            string[] indexNames = _elasticSearchDbContext.IndexNames;
            
            indexNames.ToList().ForEach(indexName =>
            {
                long newestIndexVersion = _elasticSearchDbContext.GetIndexVersion(indexName);

                IndexVersion indexVersion = GetIndexVersion(indexName);

                if (indexVersion.Version < 1)
                {
                    CreateIndexVersion(indexName, 0, newestIndexVersion);                    
                } else
                {
                    if (newestIndexVersion > indexVersion.Version)                    
                        CreateIndexVersion(indexName, indexVersion.Version, newestIndexVersion);                                           
                }
            });
        }

        private void CreateIndexVersionsIndex()
        {
            var indexVersionsIndexExists = ElasticClient.IndexExists(IndexVersionsIndexName).Exists;
            
            if (!indexVersionsIndexExists)
            {
                Console.WriteLine("Creating index versions index...");

                var createIndexResponse = ElasticClient.CreateIndex(IndexVersionsIndexName, indexVersionsIndex => indexVersionsIndex
                    .Mappings(indexVersionMapping => indexVersionMapping
                        .Map<IndexVersion>(indexVersion => indexVersion
                            .Properties(indexVersionProperty => indexVersionProperty
                                .Keyword(element => element.Name(n => n.Name))
                                .Number(element => element.Name(n => n.Version).Type(NumberType.Long))
                            )
                            .Dynamic(false)
                        )
                    )
                    .Settings(indexVersionSetting => ConfigureNonConcatenateAndAutocompleteSettings(indexVersionSetting))
                );

                if (!createIndexResponse.Acknowledged)
                    throw new Exception($"Cannot create index versions index: {createIndexResponse.ServerError}");

                Console.WriteLine("Index versions index created");
            }
        }

        private void CreateIndexVersion(string indexName, long oldVersion, long newVersion)
        {
            var createIndexDescriptor = _elasticSearchDbContext.GetCreateIndexDescriptor(this, indexName);

            string oldIndexNameWithVersion = indexName + "_v" + oldVersion;
            string newIndexNameWithVersion = indexName + "_v" + newVersion;
            
            Console.WriteLine($"Creating index {newIndexNameWithVersion}...");

            createIndexDescriptor = createIndexDescriptor.Index(newIndexNameWithVersion);

            var createIndexResponse = ElasticClient.CreateIndex(newIndexNameWithVersion, index => createIndexDescriptor);

            Console.WriteLine($"Index {newIndexNameWithVersion} created");
            
            if (oldVersion > 0)
            {
                // we have an old index, reindex

                Console.WriteLine($"Reindexing from {oldIndexNameWithVersion} to {newIndexNameWithVersion}...");

                var reindexOnServerResult = ElasticClient.ReindexOnServer(reindex => reindex
                    .Source(source => source
                        .Index(oldIndexNameWithVersion)
                    )
                    .Destination(destination => destination
                        .Index(newIndexNameWithVersion)
                        .VersionType(VersionType.Internal)
                    ));

                Console.WriteLine($"Reindexed from {oldIndexNameWithVersion} to {newIndexNameWithVersion} " +
                    $"({reindexOnServerResult.Created} created, {reindexOnServerResult.Updated} updated)");
            }

            var aliasExists = ElasticClient.AliasExists(indexName).Exists;

            if (aliasExists)
            {
                // we need to remove the old alias

                Console.WriteLine($"Deleting alias {indexName} -> {oldIndexNameWithVersion}...");

                ElasticClient.DeleteAlias(oldIndexNameWithVersion, indexName);

                Console.WriteLine($"Deleted alias {indexName} -> {oldIndexNameWithVersion}");
            }

            // we need to add the new alias

            Console.WriteLine($"Creating alias {indexName} -> {newIndexNameWithVersion}...");

            var createAliasResponse = ElasticClient.Alias(alias => alias
                .Add(action => action
                    .Index(newIndexNameWithVersion)
                    .Alias(indexName)
                )
            );

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

                    var deleteIndexResponse = ElasticClient.DeleteIndex(oldIndexNameWithVersion);

                    if (!deleteIndexResponse.Acknowledged)
                        throw new Exception($"Cannot delete old index {oldIndexNameWithVersion}");

                    Console.WriteLine($"Deleted old index {oldIndexNameWithVersion}");
                } else
                {
                    Console.WriteLine($"WARNING: do not forget to delete old index {oldIndexNameWithVersion} if everything is all right!");
                }
            }
        }

        public IPromise<IIndexSettings> ConfigureNonConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting)
        {
            return setting
                .NumberOfShards(_numberOfShards)
                .NumberOfReplicas(_numberOfReplicas)
                .Setting("index.gc_deletes", "1h"); // 1 hour
        }

        public IPromise<IIndexSettings> ConfigureConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting)
        {
            return setting
                .NumberOfShards(_numberOfShards)
                .NumberOfReplicas(_numberOfReplicas)
                .Analysis(analysis => ConfigureConcatenateAndAutocompleteAnalysis(analysis))
                .Setting("index.gc_deletes", "1h"); // 1 hour
        }

        private IAnalysis ConfigureConcatenateAndAutocompleteAnalysis(AnalysisDescriptor analysis)
        {
            return analysis
                .TokenFilters(filter => filter
                    .UserDefined("concatenate_filter", new ConcatenateTokenFilter()
                    {
                        TokenSeparator = "_",
                        IncrementGap = 1000
                    })
                    .EdgeNGram("autocomplete_filter", edgeNGram => edgeNGram
                        .MinGram(1)
                        .MaxGram(20)
                    )
                )
                .Analyzers(analyzer => analyzer
                    .Custom("autocomplete_index", custom => custom
                        .Tokenizer("standard")
                        .Filters(new string[] { "lowercase", "concatenate_filter", "autocomplete_filter" })
                    )
                    .Custom("autocomplete_search", custom => custom
                        .Tokenizer("standard")
                        .Filters(new string[] { "lowercase", "concatenate_filter" })
                    )
                );
        }

        private IndexVersion GetIndexVersion(string indexName)
        {
            bool indexVersionExists = ElasticClient.DocumentExists<IndexVersion>(indexName, get => get
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
            } else
            {
                var getIndexVersionResponse = ElasticClient.Get<IndexVersion>(indexName, get => get
                    .Index(IndexVersionsIndexName));

                return getIndexVersionResponse.Source;
            }
        }

        private void UpdateIndexVersion(IndexVersion indexVersion)
        {
            ElasticClient.Index(indexVersion, index => index
                .Index(IndexVersionsIndexName)
                .Id(indexVersion.Name));
        }
    }
}
