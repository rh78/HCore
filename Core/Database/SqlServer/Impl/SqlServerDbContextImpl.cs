using Microsoft.EntityFrameworkCore;
using System;

namespace ReinhardHolzner.HCore.Database.SqlServer.Impl
{
    public class SqlServerDbContextImpl : DbContext, ISqlServerDbContext
    {
        public const int MaxOffset = 500;
        public const int MaxPagingSize = 50;

        public string ConnectionString { get; set; }

        public SqlServerDbContextImpl(DbContextOptions options) :
            base(options)
        {
        }        
    }
}
