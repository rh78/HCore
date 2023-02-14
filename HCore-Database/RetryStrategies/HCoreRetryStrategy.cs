using System;
using System.Collections.Generic;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace HCore.Database.RetryStrategies
{
    public class HCoreRetryStrategy : NpgsqlRetryingExecutionStrategy
    {
        public HCoreRetryStrategy(DbContext context) : base(context)
        {
        }

        public HCoreRetryStrategy(ExecutionStrategyDependencies dependencies) : base(dependencies)
        {
        }

        public HCoreRetryStrategy(DbContext context, int maxRetryCount) : base(context, maxRetryCount)
        {
        }

        public HCoreRetryStrategy(ExecutionStrategyDependencies dependencies, int maxRetryCount) : base(dependencies, maxRetryCount)
        {
        }

        public HCoreRetryStrategy(DbContext context, int maxRetryCount, TimeSpan maxRetryDelay, ICollection<string> errorCodesToAdd) : base(context, maxRetryCount, maxRetryDelay, errorCodesToAdd)
        {
        }

        public HCoreRetryStrategy(ExecutionStrategyDependencies dependencies, int maxRetryCount, TimeSpan maxRetryDelay, ICollection<string> errorCodesToAdd) : base(dependencies, maxRetryCount, maxRetryDelay, errorCodesToAdd)
        {
        }

        // Code from https://github.com/dotnet/efcore/blob/main/src/EFCore/Storage/ExecutionStrategy.cs
        // Never retry on ambient transaction, as this is not supported by EF Core

        // See https://learn.microsoft.com/en-US/ef/core/miscellaneous/connection-resiliency - Option 1 "Do (almost) nothing" - we will retry on simple reads, but never on complex writes

        public override bool RetriesOnFailure 
        {
            get
            {
                if ((Dependencies.CurrentContext.Context.Database.CurrentTransaction is not null
                    || Dependencies.CurrentContext.Context.Database.GetEnlistedTransaction() is not null
                    || (((IDatabaseFacadeDependenciesAccessor)Dependencies.CurrentContext.Context.Database).Dependencies
                       .TransactionManager as ITransactionEnlistmentManager)?.CurrentAmbientTransaction is not null))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
