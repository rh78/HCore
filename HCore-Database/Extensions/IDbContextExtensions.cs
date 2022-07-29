using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HCore.Database.Extensions
{
    public static class IDbContextExtensions
    {
        public static async Task<bool?> IsAvailableAsync(this DbContext dbContext, CancellationToken cancellationToken = default)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            try
            {
                var numberOfRowsAffected = await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken).ConfigureAwait(false);

                return numberOfRowsAffected == -1;
            }
            catch (DbException)
            {
                return false;
            }
            catch (OperationCanceledException)
            {
                // The request was aborted, we don't care about the availability at this point.
                return null;
            }
        }
    }
}
