using Microsoft.EntityFrameworkCore;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;

namespace HCore.Identity.Database.SqlServer
{
    public class SqlServerConfigurationDbContext : ConfigurationDbContext
    {
        public SqlServerConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options, ConfigurationStoreOptions storeOptions) 
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseSerialColumns();

            base.OnModelCreating(modelBuilder);
        }
    }
}
