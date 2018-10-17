using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HCore.Identity.Database.SqlServer.Models.Impl;

namespace HCore.Identity.Database.SqlServer
{
    public class SqlServerIdentityDbContext : IdentityDbContext<UserModel>
    {
        public SqlServerIdentityDbContext(DbContextOptions<SqlServerIdentityDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
