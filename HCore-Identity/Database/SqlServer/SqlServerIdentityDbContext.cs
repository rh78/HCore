using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HCore.Identity.Database.SqlServer.Models.Impl;

namespace HCore.Identity.Database.SqlServer
{
    public class SqlServerIdentityDbContext : IdentityDbContext<UserModel>
    {
        public DbSet<ReservedEmailAddressModel> ReservedEmailAddresses { get; set; }
        public DbSet<DataProtectionKeyModel> DataProtectionKeys { get; set; }
        public DbSet<UserDeletedModel> UsersDeleted { get; set; }

        public SqlServerIdentityDbContext(DbContextOptions<SqlServerIdentityDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseSerialColumns();

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ReservedEmailAddressModel>()
                .HasKey(entity => new { entity.Uuid, entity.NormalizedEmailAddress });

            modelBuilder.Entity<UserModel>()
                .HasIndex(entity => entity.NormalizedEmailWithoutScope);

            modelBuilder.Entity<UserModel>()
                .HasIndex(entity => new { entity.DeveloperUuid, entity.TenantUuid, entity.Pin })
                .IsUnique();
        }
    }
}
