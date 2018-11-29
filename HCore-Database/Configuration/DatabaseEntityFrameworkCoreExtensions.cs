using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.EntityFrameworkCore
{
    public static class DatabaseEntityFrameworkCoreExtensions
    {
        public static DbContextOptionsBuilder AddSqlDatabase(this DbContextOptionsBuilder builder, string configurationKey, IConfiguration configuration, string migrationsAssembly = null)
        {
            Console.WriteLine($"Initializing SQL database context with key {configurationKey}...");

            string connectionString = configuration[$"Database:{configurationKey}:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("SQL database connection string is empty");

            builder.UseSqlServer(connectionString, options =>
            {
                if (!string.IsNullOrEmpty(migrationsAssembly))
                    options.MigrationsAssembly(migrationsAssembly);
            });

            Console.WriteLine($"Initialized SQL database context with key {configurationKey}");

            return builder;
        }
    }
}
