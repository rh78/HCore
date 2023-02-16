using HCore.Database.Models;
using HCore.Tenants.Database.SqlServer.Models.Impl;
using HCore.Tenants.Models;
using System;
using System.Threading.Tasks;

namespace HCore.Tenants.Services
{
    public interface ITenantServices
    {
        Task<ITenantInfo> CreateTenantAsync<TCustomTenantSettingsDataType>(long developerUuid, TenantSpec tenantSpec, Func<TCustomTenantSettingsDataType, Task<bool>> applyCustomTenantSettingsActionAsync)
            where TCustomTenantSettingsDataType : new();

        Task<ITenantInfo> UpdateTenantAsync<TCustomTenantSettingsDataType>(long developerUuid, long tenantUuid, TenantSpec tenantSpec, Func<TCustomTenantSettingsDataType, Task<bool>> applyCustomTenantSettingsActionAsync, int? version = null)
            where TCustomTenantSettingsDataType : new();

        Task<ITenantInfo> UpdateTenantAsync(long developerUuid, long tenantUuid, Func<TenantModel, Task<bool>> updateFuncAsync, int? version = null);

        Task<PagingResult<Tenant>> GetTenantsAsync(bool isPortals, long developerUuid, string searchTerm, int? offset, int? limit, string sortByTenant, string sortOrder);
    }
}
