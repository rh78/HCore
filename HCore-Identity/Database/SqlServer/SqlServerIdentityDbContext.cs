using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HCore.Identity.Database.SqlServer.Models.Impl;

namespace HCore.Identity.Database.SqlServer
{
    public class SqlServerIdentityDbContext : IdentityDbContext<UserModel>
    {
        public DbSet<ReservedEmailAddressModel> ReservedEmailAddresses { get; set; }
        public DbSet<DataProtectionKeyModel> DataProtectionKeys { get; set; }

        public SqlServerIdentityDbContext(DbContextOptions<SqlServerIdentityDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ReservedEmailAddressModel>()
                .HasKey(entity => new { entity.Uuid, entity.NormalizedEmailAddress });
        }
    }
}
