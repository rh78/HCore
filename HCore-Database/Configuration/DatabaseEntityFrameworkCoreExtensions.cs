using HCore.Database;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.EntityFrameworkCore
{
    public static class DatabaseEntityFrameworkCoreExtensions
    {
        public static DbContextOptionsBuilder AddSqlDatabase(this DbContextOptionsBuilder builder, string configurationKey, IConfiguration configuration, string migrationsAssembly = null)
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
                builder.UseNpgsql(connectionString, options =>
                {                   
                    if (!string.IsNullOrEmpty(migrationsAssembly))
                        options.MigrationsAssembly(migrationsAssembly);
                });
            }

            Console.WriteLine($"Initialized SQL database context with key {configurationKey}");

            return builder;
        }
    }
}
