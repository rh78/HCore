using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ReinhardHolzner.Core.Database.ElasticSearch;
using ReinhardHolzner.Core.Database.ElasticSearch.Impl;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DatabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddElasticSearch<TElasticSearchDbContext>(this IServiceCollection services, IConfiguration configuration, bool isProduction)
            where TElasticSearchDbContext : IElasticSearchDbContext
        {
            Console.WriteLine("Initializing ElasticSearch DB context...");

            // determine type here
            var elasticSearchDbContextType = typeof(TElasticSearchDbContext);

            // create an object of the type
            var elasticSearchDbContext = (TElasticSearchDbContext)Activator.CreateInstance(elasticSearchDbContextType);

            int numberOfShards = configuration.GetValue<int>("ElasticSearch:Shards");
            if (numberOfShards < 1)
                throw new Exception("ElasticSearch number of shards is invalid");

            int numberOfReplicas = configuration.GetValue<int>("ElasticSearch:Replicas");
            if (numberOfReplicas < 1)
                throw new Exception("ElasticSearch number of replicas is invalid");

            string hosts = configuration["ElasticSearch:Hosts"];
            if (string.IsNullOrEmpty(hosts))
                throw new Exception("ElasticSearch hosts not found");

            IElasticSearchClient elasticSearchClient = new ElasticSearchClientImpl(
                isProduction, numberOfShards, numberOfReplicas, hosts, elasticSearchDbContext);

            elasticSearchClient.Initialize();

            services.AddSingleton(elasticSearchClient);

            Console.WriteLine("Initialized ElasticSearch DB context");

            return services;
        }

        public static IServiceCollection AddSqlServer<TSqlServerDbContext>(this IServiceCollection services, string configurationKey, IConfiguration configuration)
            where TSqlServerDbContext : DbContext
        {
            Console.WriteLine($"Initializing SQL Server DB context with key {configurationKey}...");

            string connectionString = configuration[$"SqlServer:{configurationKey}:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("SQL Server connection string is empty");

            services.AddDbContext<TSqlServerDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            Console.WriteLine($"Initialized SQL Server DB context with key {configurationKey}");

            return services;
        }
    }
}
