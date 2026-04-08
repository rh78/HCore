/* TODO remove
 
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;

namespace HCore.Identity.Database.SqlServer
{
    public class SqlServerPersistedGrantDbContext : PersistedGrantDbContext
    {
        public SqlServerPersistedGrantDbContext(DbContextOptions<PersistedGrantDbContext> options, OperationalStoreOptions storeOptions) 
            : base(options)
        {
            this.StoreOptions = storeOptions;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseSerialColumns();

            base.OnModelCreating(modelBuilder);
        }
    }
}

*/