using HCore.Database.Models;
using HCore.Tenants.Models;
using System;
using System.Threading.Tasks;

namespace HCore.Tenants.Services
{
    public interface ITenantServices
    {
        Task<ITenantInfo> CreateTenantAsync<TCustomTenantSettingsDataType>(long developerUuid, TenantSpec tenantSpec, Func<TCustomTenantSettingsDataType, bool> applyCustomTenantSettingsAction)
            where TCustomTenantSettingsDataType : new();

        Task<ITenantInfo> UpdateTenantAsync<TCustomTenantSettingsDataType>(long developerUuid, long tenantUuid, TenantSpec tenantSpec, Func<TCustomTenantSettingsDataType, bool> applyCustomTenantSettingsAction, int? version = null)
            where TCustomTenantSettingsDataType : new();

        Task<PagingResult<Tenant>> GetTenantsAsync(bool isPortals, long developerUuid, string searchTerm, int? offset, int? limit, string sortByTenant, string sortOrder);
    }
}
