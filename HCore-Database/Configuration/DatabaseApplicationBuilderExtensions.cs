using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class DatabaseApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSqlDatabase<TContext>(this IApplicationBuilder app, bool migrate = true)
            where TContext : DbContext
        {
            var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

                if (migrate)
                {
                    // index builds on large tables take far longer than the default 30 s command timeout; when the
                    // client cancels a CREATE INDEX CONCURRENTLY, Postgres keeps an invalid index behind. This only
                    // applies to this scoped context, not to regular queries

                    dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

                    dbContext.Database.Migrate();
                }
            }

            return app;
        }
    }
}