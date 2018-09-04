using Elasticsearch.Net;
using Nest;
using ReinhardHolzner.HCore.ElasticSearch.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReinhardHolzner.HCore.ElasticSearch.Impl
{
    public class ElasticSearchClient : IElasticSearchClient
    {
        private const string INDEX_VERSIONS_INDEX_NAME = "indexVersions";

        private int _numberOfShards;
        private int _numberOfReplicas;
        private string _hosts;        

        private ElasticClient _elasticClient;

        private IElasticSearchMappingInterface _mappingInterface;

        public ElasticSearchClient(int numberOfShards, int numberOfReplicas, string hosts, IElasticSearchMappingInterface mappingInterface)
        {
            _numberOfShards = numberOfShards;
            _numberOfReplicas = numberOfReplicas;
            _hosts = hosts;            

            _mappingInterface = mappingInterface;
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
                int port = 9300;

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

                Uri uri = new Uri("http://" + host + ":" + port);
                uriList.Add(uri);
            });

            var connectionPool = new SniffingConnectionPool(uriList);
            var settings = new ConnectionSettings(connectionPool)
                .DisableAutomaticProxyDetection();

            _elasticClient = new ElasticClient(settings);

            CreateIndexVersionsIndex();

            string[] indexNames = _mappingInterface.IndexNames;
            
            indexNames.ToList().ForEach(indexName =>
            {
                bool indexExists = _elasticClient.IndexExists(indexName).Exists;
                if (!indexExists)
                {
                    _mappingInterface.CreateIndex(this, indexName);
                } else
                {
                    IndexVersion indexVersion = GetIndexVersion(indexName);

                    long newIndexVersion = _mappingInterface.UpdateIndex(this, indexName, indexVersion.Version);

                    if (newIndexVersion > indexVersion.Version)
                    {
                        indexVersion.Version = newIndexVersion;

                        UpdateIndexVersion(indexVersion);
                    }
                }
            });
        }

        private void CreateIndexVersionsIndex()
        {
            bool indexVersionsIndexExists = _elasticClient.IndexExists(INDEX_VERSIONS_INDEX_NAME).Exists;

            if (!indexVersionsIndexExists)
            {
                Console.WriteLine("Creating index versions index...");

                var createIndexResponse = _elasticClient.CreateIndex(INDEX_VERSIONS_INDEX_NAME, indexVersionsIndex => indexVersionsIndex
                    .Mappings(indexVersionMapping => indexVersionMapping
                        .Map<IndexVersion>(indexVersion => indexVersion
                            .Properties(indexVersionProperty => indexVersionProperty
                                .Text(element => element.Name(n => n.Name))
                                .Number(element => element.Name(n => n.Version).Type(NumberType.Long))
                            )
                            .Dynamic(false)
                        )
                    )
                    .Settings(indexVersionSetting => ConfigureNonConcatenateAndAutocompleteSettings(indexVersionSetting))
                );

                if (!createIndexResponse.Acknowledged)
                    throw new Exception("Cannot create index versions index: " + createIndexResponse.ServerError);

                Console.WriteLine("Index versions index created");
            }
        }

        private IPromise<IIndexSettings> ConfigureNonConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting)
        {
            return setting
                .NumberOfShards(_numberOfShards)
                .NumberOfReplicas(_numberOfReplicas);
        }

        private IPromise<IIndexSettings> ConfigureConcatenateAndAutocompleteSettings(IndexSettingsDescriptor setting)
        {
            return setting
                .NumberOfShards(_numberOfShards)
                .NumberOfReplicas(_numberOfReplicas)
                .Analysis(analysis => ConfigureConcatenateAndAutocompleteAnalysis(analysis));
        }

        public IAnalysis ConfigureConcatenateAndAutocompleteAnalysis(AnalysisDescriptor analysis)
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
            var getIndexVersionResponse = _elasticClient.Get<IndexVersion>(indexName, get => get
                .Index(INDEX_VERSIONS_INDEX_NAME));

            if (!getIndexVersionResponse.Found)
            {
                var newIndexVersion = new IndexVersion()
                {
                    Name = indexName,
                    Version = 1
                };

                UpdateIndexVersion(newIndexVersion);

                return newIndexVersion;
            } else
            {
                return getIndexVersionResponse.Source;
            }
        }

        private void UpdateIndexVersion(IndexVersion indexVersion)
        {
            _elasticClient.Index(indexVersion, index => index
                .Index(INDEX_VERSIONS_INDEX_NAME)
                .Index(indexVersion.Name));
        }
    }
}
