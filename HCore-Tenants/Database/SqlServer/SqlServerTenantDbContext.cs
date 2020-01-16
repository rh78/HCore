using Microsoft.EntityFrameworkCore;
using HCore.Tenants.Database.SqlServer.Models.Impl;

namespace HCore.Tenants.Database.SqlServer
{
    public class SqlServerTenantDbContext : DbContext
    {
        public DbSet<DeveloperModel> Developers { get; set; }
        public DbSet<TenantModel> Tenants { get; set; }
        
        public SqlServerTenantDbContext(DbContextOptions<SqlServerTenantDbContext> options)
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
