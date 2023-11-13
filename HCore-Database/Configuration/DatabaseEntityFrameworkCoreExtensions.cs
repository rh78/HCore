using System;
using System.Collections.Concurrent;
using HCore.Database;
using HCore.Database.RetryStrategies;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Microsoft.EntityFrameworkCore
{
    public static class DatabaseEntityFrameworkCoreExtensions
    {
        private static readonly ConcurrentDictionary<string, NpgsqlDataSource> _sqlDataSourcesByConfigurationKey = new();

        public static DbContextOptionsBuilder AddSqlDatabase(this DbContextOptionsBuilder builder, string configurationKey, IConfiguration configuration, string migrationsAssembly = null)
        {
            string implementation = configuration[$"Database:{configurationKey}:Implementation"];

            if (string.IsNullOrEmpty(implementation))
                throw new Exception("Database implementation specification is empty");

            if (!implementation.Equals(DatabaseConstants.DatabaseImplementationSqlServer) && !implementation.Equals(DatabaseConstants.DatabaseImplementationPostgres))
                throw new Exception("Database implementation specification is invalid");

            string connectionString = configuration[$"Database:{configurationKey}:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("SQL database connection string is empty");

            if (implementation.Equals(DatabaseConstants.DatabaseImplementationSqlServer))
            {
                builder.UseSqlServer(connectionString, options =>
                {
                    if (!string.IsNullOrEmpty(migrationsAssembly))
                        options.MigrationsAssembly(migrationsAssembly);
                });
            }
            else
            {
                var dataSource = _sqlDataSourcesByConfigurationKey.GetOrAdd(
                    configurationKey,
                    (_) =>
                    {
                        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

                        dataSourceBuilder.EnableDynamicJsonMappings();

                        return dataSourceBuilder.Build();
                    });

                builder.UseNpgsql(
                    dataSource,
                    options =>
                    {
                        if (!string.IsNullOrEmpty(migrationsAssembly))
                            options.MigrationsAssembly(migrationsAssembly);

                        options.ExecutionStrategy((ExecutionStrategyDependencies c) => new HCoreRetryStrategy(c));
                    });
            }

            return builder;
        }
    }
}
