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
                    dbContext.Database.Migrate();
                }
            }

            return app;
        }
    }
}