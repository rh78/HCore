using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class DatabaseApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSqlServer<TSqlServerDbContext>(this IApplicationBuilder app)
            where TSqlServerDbContext : DbContext
        {
            var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<TSqlServerDbContext>();
            }

            return app;
        }
    }
}