using Microsoft.EntityFrameworkCore;
using HCore.Tenants.Database.SqlServer.Models.Impl;

namespace HCore.Tenants.Database.SqlServer
{
    public class SqlServerTenantDbContext : DbContext
    {
        public DbSet<DeveloperModel> Developers { get; set; }
        public DbSet<TenantModel> Tenants { get; set; }
        public DbSet<SubscriptionModel> Subscriptions { get; set; }

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

            modelBuilder.Entity<TenantModel>()
                .Property(entity => entity.OidcQueryUserInfoEndpoint)
                .HasDefaultValue(true);

            modelBuilder.Entity<TenantModel>()
                .HasMany(entity => entity.Subscriptions)
                .WithOne(entity => entity.Tenant)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubscriptionModel>()
                .HasIndex(entity => entity.ExternalSubscriptionUuid);

            base.OnModelCreating(modelBuilder);            
        }
    }
}
