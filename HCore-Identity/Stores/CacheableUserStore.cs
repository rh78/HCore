using System.Threading;
using System.Threading.Tasks;
using HCore.Identity.Database.SqlServer;
using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.Providers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HCore.Identity.Stores
{
    public class CacheableUserStore : UserStore<UserModel>
    {
        private readonly ISecurityStampCacheProvider _securityStampCacheProvider;

        public CacheableUserStore(SqlServerIdentityDbContext context, IdentityErrorDescriber describer = null) : base(context, describer)
        {
            _securityStampCacheProvider = context.GetService<ISecurityStampCacheProvider>();
        }

        public override async Task<IdentityResult> UpdateAsync(UserModel user, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await base.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

            if (result.Succeeded && _securityStampCacheProvider != null)
            {
                await _securityStampCacheProvider.CreateOrUpdateSecurityStampAsync(user.Id, user.SecurityStamp).ConfigureAwait(false);
            }

            return result;
        }

        public override async Task<IdentityResult> DeleteAsync(UserModel user, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await base.DeleteAsync(user, cancellationToken).ConfigureAwait(false);

            if (result.Succeeded && _securityStampCacheProvider != null)
            {
                await _securityStampCacheProvider.InvalidateSecurityStampAsync(user.Id).ConfigureAwait(false);
            }

            return result;
        }
    }
}
