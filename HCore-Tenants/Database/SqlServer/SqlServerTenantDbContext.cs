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

            modelBuilder.Entity<DeveloperModel>()
                .HasMany(entity => entity.Tenants)
                .WithOne(entity => entity.Developer);

            modelBuilder.Entity<TenantModel>()
                .HasIndex(entity => new { entity.DeveloperUuid, entity.Uuid });

            modelBuilder.Entity<TenantModel>()
                .HasIndex(entity => new { entity.DeveloperUuid, entity.SubdomainPatterns })
                .IsUnique();

            base.OnModelCreating(modelBuilder);            
        }
    }
}
