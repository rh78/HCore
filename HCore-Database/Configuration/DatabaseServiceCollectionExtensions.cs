using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HCore.Database.ElasticSearch;
using HCore.Database.ElasticSearch.Impl;
using System;
using System.Reflection;
using HCore.Database;

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

        public static IServiceCollection AddSqlDatabase<TStartup, TContext>(this IServiceCollection services, string configurationKey, IConfiguration configuration)
            where TContext : DbContext
        {
            Console.WriteLine($"Initializing SQL database context with key {configurationKey}...");

            string implementation = configuration[$"Database:{configurationKey}:Implementation"];

            if (string.IsNullOrEmpty(implementation))
                throw new Exception("Database implementation specification is empty");

            if (!implementation.Equals(DatabaseConstants.DatabaseImplementationSqlServer) && !implementation.Equals(DatabaseConstants.DatabaseImplementationPostgres))
                throw new Exception("Database implementation specification is invalid");

            string connectionString = configuration[$"Database:{configurationKey}:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("SQL database connection string is empty");

            var migrationsAssembly = typeof(TStartup).GetTypeInfo().Assembly.GetName().Name;

            if (implementation.Equals(DatabaseConstants.DatabaseImplementationSqlServer))
            {
                services.AddDbContext<TContext>(options =>
                {
                    options.UseSqlServer(connectionString,
                        sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly));
                });
            } 
            else
            {
                services.AddDbContext<TContext>(options =>
                {
                    options.UseNpgsql(connectionString,
                        postgresOptions => postgresOptions.MigrationsAssembly(migrationsAssembly));
                });
            }

            Console.WriteLine($"Initialized SQL database context with key {configurationKey}");

            return services;
        }
    }
}
