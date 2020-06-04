using HCore.Database.Models;
using HCore.Tenants.Models;
using System.Threading.Tasks;

namespace HCore.Tenants.Services
{
    public interface ITenantServices
    {
        Task<ITenantInfo> CreateTenantAsync(long developerUuid, TenantSpec tenantSpec);

        Task<ITenantInfo> UpdateTenantAsync(long developerUuid, long tenantUuid, TenantSpec tenantSpec);

        Task<PagingResult<Tenant>> GetTenantsAsync(long developerUuid, string searchTerm, int? offset, int? limit, string sortByTenant, string sortOrder);
    }
}
