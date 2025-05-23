﻿using ExCSS;
using HCore.Database.Models;
using HCore.Tenants.Cache;
using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Database.SqlServer.Models.Impl;
using HCore.Tenants.Models;
using HCore.Tenants.Providers;
using HCore.Web.API.Impl;
using HCore.Web.Exceptions;
using HCore.Web.Providers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HCore.Tenants.Services.Impl
{
    public class TenantServicesImpl : ITenantServices
    {
        public static readonly Regex SafeString = new Regex(@"^[\w\s\.@_\-\+\=\(\):/&,]+$");
        private static readonly Regex Tenant = new Regex(@"^[a-zA-Z0-9\-]+$");

        public const int MaxEmailAddressLength = 255;
        public const int MaxEmailDisplayNameLength = 50;

        private readonly ITenantDataProvider _tenantDataProvider;

        private readonly ITenantCache _tenantCache;

        private readonly INowProvider _nowProvider;

        private readonly SqlServerTenantDbContext _dbContext;

        public TenantServicesImpl(SqlServerTenantDbContext dbContext, ITenantDataProvider tenantDataProvider, ITenantCache tenantCache, INowProvider nowProvider)
        {
            _dbContext = dbContext;

            _tenantDataProvider = tenantDataProvider;

            _tenantCache = tenantCache;

            _nowProvider = nowProvider;
        }

        public async Task<ITenantInfo> CreateTenantAsync<TCustomTenantSettingsDataType>(long developerUuid, TenantSpec tenantSpec, Func<TCustomTenantSettingsDataType, Task<bool>> applyCustomTenantSettingsActionAsync)
            where TCustomTenantSettingsDataType: new()
        {
            var tenantModel = new TenantModel();

            var developerInfo = _tenantDataProvider.GetDeveloper(developerUuid);

            if (developerInfo == null)
                throw new NotFoundApiException(NotFoundApiException.DeveloperNotFound, $"Developer with UUID {developerUuid} was not found", Convert.ToString(developerUuid));

            tenantModel.DeveloperUuid = developerInfo.DeveloperUuid;

            string subdomain = ProcessSubdomain(tenantSpec.Subdomain);

            var (_, tenantInfo) = await _tenantDataProvider.GetTenantByHostAsync($"{subdomain}.{developerInfo.HostPattern}").ConfigureAwait(false);

            if (tenantInfo != null)
            {
                // lets check if the creator is the same for the existing subdomain
                // this makes the call re-entrant, if something happens later on

                if (!string.IsNullOrEmpty(tenantInfo.CreatedByUserUuid) &&
                    !string.IsNullOrEmpty(tenantSpec.CreatedByUserUuid) &&
                    string.Equals(tenantInfo.CreatedByUserUuid, tenantSpec.CreatedByUserUuid))
                {
                    // make it reentrant

                    return tenantInfo;
                }

                throw new RequestFailedApiException(RequestFailedApiException.TenantSubdomainAlreadyUsed, "This subdomain is already being used");
            }

            tenantModel.SubdomainPatterns = new string[] { subdomain };

            if (!string.IsNullOrEmpty(developerInfo.DefaultEcbBackendApiUrlSuffix))
            {
                tenantModel.EcbBackendApiUrl = $"https://{subdomain}.{developerInfo.DefaultEcbBackendApiUrlSuffix}";
            }
            else
            {
                tenantModel.EcbBackendApiUrl = null;
            }

            tenantModel.PortalsBackendApiUrl = $"https://{subdomain}.{developerInfo.DefaultPortalsBackendApiUrlSuffix}";

            tenantModel.FrontendApiUrl = $"https://{subdomain}.{developerInfo.DefaultFrontendApiUrlSuffix}";
            tenantModel.WebUrl = $"https://{subdomain}.{developerInfo.DefaultWebUrlSuffix}";

            tenantModel.Name = ProcessName(tenantSpec.Name);

            if (tenantSpec.LogoSvgUrlSet)
            {
                string logoSvgUrl = ProcessLogoSvgUrl(tenantSpec.LogoSvgUrl);

                tenantModel.LogoSvgUrl = logoSvgUrl;
            }

            if (tenantSpec.LogoPngUrlSet)
            {
                string logoPngUrl = ProcessLogoPngUrl(tenantSpec.LogoPngUrl);

                tenantModel.LogoPngUrl = logoPngUrl;
            }

            if (tenantSpec.IconIcoUrlSet)
            {
                string iconIcoUrl = ProcessIconIcoUrl(tenantSpec.IconIcoUrl);

                tenantModel.IconIcoUrl = iconIcoUrl;
            }

            if (tenantSpec.AppleTouchIconUrlSet)
            {
                string appleTouchIconUrl = ProcessAppleTouchIconUrl(tenantSpec.AppleTouchIconUrl);

                tenantModel.AppleTouchIconUrl = appleTouchIconUrl;
            }

            if (tenantSpec.CustomCssSet)
            {
                string customCss = await ProcessCustomCssAsync(tenantSpec.CustomCss, optimizeCustomCss: true);

                tenantModel.CustomCss = customCss;
            }

            if (tenantSpec.PrimaryColorSet)
            {
                int? primaryColor = ProcessPrimaryColor(tenantSpec.PrimaryColor);

                tenantModel.PrimaryColor = primaryColor;
            }

            if (tenantSpec.SecondaryColorSet)
            {
                int? secondaryColor = ProcessSecondaryColor(tenantSpec.SecondaryColor);

                tenantModel.SecondaryColor = secondaryColor;
            }

            if (tenantSpec.TextOnPrimaryColorSet)
            {
                int? textOnPrimaryColor = ProcessTextOnPrimaryColor(tenantSpec.TextOnPrimaryColor);

                tenantModel.TextOnPrimaryColor = textOnPrimaryColor;
            }

            if (tenantSpec.TextOnSecondaryColorSet)
            {
                int? textOnSecondaryColor = ProcessTextOnSecondaryColor(tenantSpec.TextOnSecondaryColor);

                tenantModel.TextOnSecondaryColor = textOnSecondaryColor;
            }

            if (tenantSpec.SupportEmailSet)
            {
                string supportEmail = ProcessSupportEmail(tenantSpec.SupportEmail);

                tenantModel.SupportEmail = supportEmail;
            }

            if (tenantSpec.SupportEmailDisplayNameSet)
            {
                string supportEmailDisplayName = ProcessSupportEmailDisplayName(tenantSpec.SupportEmailDisplayName);

                tenantModel.SupportEmailDisplayName = supportEmailDisplayName;
            }

            if (tenantSpec.NoreplyEmailSet)
            {
                string noreplyEmail = ProcessNoreplyEmail(tenantSpec.NoreplyEmail);

                tenantModel.NoreplyEmail = noreplyEmail;
            }

            if (tenantSpec.NoreplyEmailDisplayNameSet)
            {
                string noreplyEmailDisplayName = ProcessNoreplyEmailDisplayName(tenantSpec.NoreplyEmailDisplayName);

                tenantModel.NoreplyEmailDisplayName = noreplyEmailDisplayName;
            }

            if (tenantSpec.DefaultCultureSet)
            {
                string defaultCulture = ProcessDefaultCulture(tenantSpec.DefaultCulture);

                tenantModel.DefaultCulture = defaultCulture;
            }

            if (tenantSpec.DefaultCurrencySet)
            {
                string defaultCurrency = ProcessDefaultCurrency(tenantSpec.DefaultCurrency);

                tenantModel.DefaultCurrency = defaultCurrency;
            }

            if (tenantSpec.EnableAuditSet)
            {
                bool enableAudit = ProcessEnableAudit(tenantSpec.EnableAudit);

                tenantModel.EnableAudit = enableAudit;
            }

            tenantModel.CreatedByUserUuid = ApiImpl.ProcessUserUuid(tenantSpec.CreatedByUserUuid);

            var customTenantSettings = new TCustomTenantSettingsDataType();

            var apply = await applyCustomTenantSettingsActionAsync(customTenantSettings).ConfigureAwait(false);

            if (apply)
            {
                tenantModel.SetCustomTenantSettings(customTenantSettings);
            }

            tenantModel.CreatedAt = _nowProvider.Now;
            tenantModel.Version = 1;

            try
            { 
                _dbContext.Tenants.Add(tenantModel);

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateException e)
            when (e.InnerException is SqlException sqlEx &&
                 (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                // only one allowed
                throw new RequestFailedApiException(RequestFailedApiException.TenantSubdomainAlreadyUsed, "This subdomain is already being used");
            }
            catch (DbUpdateException e)
            when (e.InnerException is PostgresException postgresEx &&
                  postgresEx.SqlState == "23505")
            {
                // only one allowed
                throw new RequestFailedApiException(RequestFailedApiException.TenantSubdomainAlreadyUsed, "This subdomain is already being used");
            }

            tenantInfo = await _tenantDataProvider.GetTenantByUuidThrowAsync(developerUuid, tenantModel.Uuid).ConfigureAwait(false);

            return tenantInfo;
        }

        public async Task<ITenantInfo> UpdateTenantAsync(long developerUuid, long tenantUuid, Func<TenantModel, Task<(bool changed, Func<TenantModel, TenantModel, Task> logAuditFunc)>> updateFuncAsync, int? version = null)
        {
            using (var transaction = await _dbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
            {
                // now lets diff

                IQueryable<TenantModel> query = _dbContext.Tenants;

                query = query.Where(tenant => tenant.DeveloperUuid == developerUuid);
                query = query.Where(tenant => tenant.Uuid == tenantUuid);

                var tenantModel = await query.FirstOrDefaultAsync().ConfigureAwait(false);

                if (tenantModel == null)
                    throw new NotFoundApiException(NotFoundApiException.TenantNotFound, $"Tenant with UUID {tenantUuid} was not found", Convert.ToString(tenantUuid));

                if (version != null && tenantModel.Version != version)
                    throw new OptimisticLockingApiException(OptimisticLockingApiException.TenantOptimisticLockViolated, "The tenant was modified by another user, please try again");

                var originalTenantModel = tenantModel.Clone();

                var (changed, logAuditFunc) = await updateFuncAsync(tenantModel).ConfigureAwait(false);

                if (changed)
                {
                    tenantModel.Version++;
                    tenantModel.LastUpdatedAt = _nowProvider.Now;

                    try
                    {
                        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                        // can change legal entity names                        

                        transaction.Commit();

                        await _tenantCache.InvalidateTenantInfosAsync(tenantModel.DeveloperUuid, tenantModel.Uuid).ConfigureAwait(false);

                        if (logAuditFunc != null)
                        {
                            await logAuditFunc(originalTenantModel, tenantModel).ConfigureAwait(false);
                        }
                    }
                    catch (DbUpdateException e)
                    when (e.InnerException is SqlException sqlEx &&
                         (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                    {
                        // only one allowed
                        throw new RequestFailedApiException(RequestFailedApiException.TenantSubdomainAlreadyUsed, "This subdomain is already being used");
                    }
                    catch (DbUpdateException e)
                    when (e.InnerException is PostgresException postgresEx &&
                          postgresEx.SqlState == "23505")
                    {
                        // only one allowed
                        throw new RequestFailedApiException(RequestFailedApiException.TenantSubdomainAlreadyUsed, "This subdomain is already being used");
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        throw new OptimisticLockingApiException(OptimisticLockingApiException.TenantOptimisticLockViolated, "The tenant was modified by another user, please try again");
                    }
                }
            }

            var tenantInfo = await _tenantDataProvider.GetTenantByUuidThrowAsync(developerUuid, tenantUuid).ConfigureAwait(false);

            return tenantInfo;
        }

        public async Task<ITenantInfo> UpdateTenantAsync<TCustomTenantSettingsDataType>(long developerUuid, long tenantUuid, TenantSpec tenantSpec, Func<TCustomTenantSettingsDataType, Task<bool>> applyCustomTenantSettingsActionAsync, Func<TenantModel, TenantModel, Task> logAuditFunc = null, int? version = null)
            where TCustomTenantSettingsDataType : new()
        {
#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
            return await UpdateTenantAsync(developerUuid, tenantUuid, async (tenantModelForUpdate) =>
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
            {
                bool changed = false;

                var developerInfo = _tenantDataProvider.GetDeveloper(developerUuid);

                if (developerInfo == null)
                    throw new NotFoundApiException(NotFoundApiException.DeveloperNotFound, $"Developer with UUID {developerUuid} was not found", Convert.ToString(developerUuid));

                bool subdomainChanged = false;

                if (tenantSpec.SubdomainSet)
                {
                    string subdomain = ProcessSubdomain(tenantSpec.Subdomain);

                    if (tenantModelForUpdate.SubdomainPatterns == null || tenantModelForUpdate.SubdomainPatterns.Length == 0)
                    {
                        var (_, tenantInfo) = await _tenantDataProvider.GetTenantByHostAsync($"{subdomain}{developerInfo.HostPattern}").ConfigureAwait(false);

                        if (tenantInfo != null)
                            throw new RequestFailedApiException(RequestFailedApiException.TenantSubdomainAlreadyUsed, "This subdomain is already being used");

                        tenantModelForUpdate.SubdomainPatterns = new string[] { subdomain };

                        subdomainChanged = true;

                        changed = true;
                    }
                    else if (tenantModelForUpdate.SubdomainPatterns.Length > 1)
                    {
                        throw new RequestFailedApiException(RequestFailedApiException.TenantSubdomainImmutable, "The tenant subdomain for this tenant is immutable");
                    }
                    else if (!string.Equals(tenantModelForUpdate.SubdomainPatterns[0], subdomain))
                    {
                        var (_, tenantInfo) = await _tenantDataProvider.GetTenantByHostAsync($"{subdomain}{developerInfo.HostPattern}").ConfigureAwait(false);

                        if (tenantInfo != null)
                            throw new RequestFailedApiException(RequestFailedApiException.TenantSubdomainAlreadyUsed, "This subdomain is already being used");

                        tenantModelForUpdate.SubdomainPatterns = new string[] { subdomain };

                        subdomainChanged = true;

                        changed = true;
                    }

                    if (subdomainChanged)
                    {
                        if (!string.IsNullOrEmpty(developerInfo.DefaultEcbBackendApiUrlSuffix))
                        {
                            tenantModelForUpdate.EcbBackendApiUrl = $"https://{subdomain}.{developerInfo.DefaultEcbBackendApiUrlSuffix}";
                        }
                        else
                        {
                            tenantModelForUpdate.EcbBackendApiUrl = null;
                        }

                        tenantModelForUpdate.PortalsBackendApiUrl = $"https://{subdomain}.{developerInfo.DefaultPortalsBackendApiUrlSuffix}";

                        tenantModelForUpdate.FrontendApiUrl = $"https://{subdomain}.{developerInfo.DefaultFrontendApiUrlSuffix}";
                        tenantModelForUpdate.WebUrl = $"https://{subdomain}.{developerInfo.DefaultWebUrlSuffix}";
                    }
                }

                if (tenantSpec.NameSet)
                {
                    string name = ProcessName(tenantSpec.Name);

                    if (!string.Equals(tenantModelForUpdate.Name, name))
                    {
                        tenantModelForUpdate.Name = name;

                        changed = true;
                    }
                }

                if (tenantSpec.LogoSvgUrlSet)
                {
                    string logoSvgUrl = ProcessLogoSvgUrl(tenantSpec.LogoSvgUrl);

                    if (!string.Equals(tenantModelForUpdate.LogoSvgUrl, logoSvgUrl))
                    {
                        tenantModelForUpdate.LogoSvgUrl = logoSvgUrl;

                        changed = true;
                    }
                }

                if (tenantSpec.LogoPngUrlSet)
                {
                    string logoPngUrl = ProcessLogoPngUrl(tenantSpec.LogoPngUrl);

                    if (!string.Equals(tenantModelForUpdate.LogoPngUrl, logoPngUrl))
                    {
                        tenantModelForUpdate.LogoPngUrl = logoPngUrl;

                        changed = true;
                    }
                }

                if (tenantSpec.IconIcoUrlSet)
                {
                    string iconIcoUrl = ProcessIconIcoUrl(tenantSpec.IconIcoUrl);

                    if (!string.Equals(tenantModelForUpdate.IconIcoUrl, iconIcoUrl))
                    {
                        tenantModelForUpdate.IconIcoUrl = iconIcoUrl;

                        changed = true;
                    }
                }

                if (tenantSpec.AppleTouchIconUrlSet)
                {
                    string appleTouchIconUrl = ProcessAppleTouchIconUrl(tenantSpec.AppleTouchIconUrl);

                    if (!string.Equals(tenantModelForUpdate.AppleTouchIconUrl, appleTouchIconUrl))
                    {
                        tenantModelForUpdate.AppleTouchIconUrl = appleTouchIconUrl;

                        changed = true;
                    }
                }

                if (tenantSpec.CustomCssSet)
                {
                    string customCss = await ProcessCustomCssAsync(tenantSpec.CustomCss, optimizeCustomCss: true);

                    if (!string.Equals(tenantModelForUpdate.CustomCss, customCss))
                    {
                        tenantModelForUpdate.CustomCss = customCss;

                        changed = true;
                    }
                }

                if (tenantSpec.PrimaryColorSet)
                {
                    int? primaryColor = ProcessPrimaryColor(tenantSpec.PrimaryColor);

                    if (tenantModelForUpdate.PrimaryColor != primaryColor)
                    {
                        tenantModelForUpdate.PrimaryColor = primaryColor;

                        changed = true;
                    }
                }

                if (tenantSpec.SecondaryColorSet)
                {
                    int? secondaryColor = ProcessSecondaryColor(tenantSpec.SecondaryColor);

                    if (tenantModelForUpdate.SecondaryColor != secondaryColor)
                    {
                        tenantModelForUpdate.SecondaryColor = secondaryColor;

                        changed = true;
                    }
                }

                if (tenantSpec.TextOnPrimaryColorSet)
                {
                    int? textOnPrimaryColor = ProcessTextOnPrimaryColor(tenantSpec.TextOnPrimaryColor);

                    if (tenantModelForUpdate.TextOnPrimaryColor != textOnPrimaryColor)
                    {
                        tenantModelForUpdate.TextOnPrimaryColor = textOnPrimaryColor;

                        changed = true;
                    }
                }

                if (tenantSpec.TextOnSecondaryColorSet)
                {
                    int? textOnSecondaryColor = ProcessTextOnSecondaryColor(tenantSpec.TextOnSecondaryColor);

                    if (tenantModelForUpdate.TextOnSecondaryColor != textOnSecondaryColor)
                    {
                        tenantModelForUpdate.TextOnSecondaryColor = textOnSecondaryColor;

                        changed = true;
                    }
                }

                if (tenantSpec.SupportEmailSet)
                {
                    string supportEmail = ProcessSupportEmail(tenantSpec.SupportEmail);

                    if (!string.Equals(tenantModelForUpdate.SupportEmail, supportEmail))
                    {
                        tenantModelForUpdate.SupportEmail = supportEmail;

                        changed = true;
                    }
                }

                if (tenantSpec.SupportEmailDisplayNameSet)
                {
                    string supportEmailDisplayName = ProcessSupportEmailDisplayName(tenantSpec.SupportEmailDisplayName);

                    if (!string.Equals(tenantModelForUpdate.SupportEmailDisplayName, supportEmailDisplayName))
                    {
                        tenantModelForUpdate.SupportEmailDisplayName = supportEmailDisplayName;

                        changed = true;
                    }
                }

                if (tenantSpec.NoreplyEmailSet)
                {
                    string noreplyEmail = ProcessNoreplyEmail(tenantSpec.NoreplyEmail);

                    if (!string.Equals(tenantModelForUpdate.NoreplyEmail, noreplyEmail))
                    {
                        tenantModelForUpdate.NoreplyEmail = noreplyEmail;

                        changed = true;
                    }
                }

                if (tenantSpec.NoreplyEmailDisplayNameSet)
                {
                    string noreplyEmailDisplayName = ProcessNoreplyEmailDisplayName(tenantSpec.NoreplyEmailDisplayName);

                    if (!string.Equals(tenantModelForUpdate.NoreplyEmailDisplayName, noreplyEmailDisplayName))
                    {
                        tenantModelForUpdate.NoreplyEmailDisplayName = noreplyEmailDisplayName;

                        changed = true;
                    }
                }

                if (tenantSpec.DefaultCultureSet)
                {
                    string defaultCulture = ProcessDefaultCulture(tenantSpec.DefaultCulture);

                    if (!string.Equals(tenantModelForUpdate.DefaultCulture, defaultCulture))
                    {
                        tenantModelForUpdate.DefaultCulture = defaultCulture;

                        changed = true;
                    }
                }

                if (tenantSpec.DefaultCurrencySet)
                {
                    string defaultCurrency = ProcessDefaultCurrency(tenantSpec.DefaultCurrency);

                    if (!string.Equals(tenantModelForUpdate.DefaultCurrency, defaultCurrency))
                    {
                        tenantModelForUpdate.DefaultCurrency = defaultCurrency;

                        changed = true;
                    }
                }

                if (tenantSpec.EnableAuditSet)
                {
                    bool enableAudit = ProcessEnableAudit(tenantSpec.EnableAudit);

                    if (tenantModelForUpdate.EnableAudit != enableAudit)
                    {
                        tenantModelForUpdate.EnableAudit = enableAudit;

                        changed = true;
                    }
                }

                var customTenantSettings = tenantModelForUpdate.GetCustomTenantSettings<TCustomTenantSettingsDataType>();

                if (customTenantSettings == null)
                    customTenantSettings = new TCustomTenantSettingsDataType();

                bool locallyChanged = await applyCustomTenantSettingsActionAsync(customTenantSettings).ConfigureAwait(false);
                
                if (locallyChanged)
                {
                    tenantModelForUpdate.SetCustomTenantSettings(customTenantSettings);

                    changed = true;
                }

                return (changed, logAuditFunc);
            }, version).ConfigureAwait(false);
        }

        public async Task<PagingResult<Tenant>> GetTenantsAsync(bool isPortals, long developerUuid, string searchTerm, int? offset, int? limit, string sortByTenant, string sortOrder)
        {
            int offsetInt = HCore.Database.API.Impl.ApiImpl.ProcessSqlServerPagingOffset(offset, limit);
            int limitInt = HCore.Database.API.Impl.ApiImpl.ProcessSqlServerPagingLimit(offset, limit);

            var sortByEnum = ProcessSortByTenant(sortByTenant);
            var sortOrderEnum = ProcessSortOrder(sortOrder);

            var developerInfo = _tenantDataProvider.GetDeveloper(developerUuid);

            if (developerInfo == null)
            {
                return new PagingResult<Tenant>()
                {
                    TotalCount = 0,
                    Result = new List<Tenant>()
                };
            }

            var pagingResult = new PagingResult<Tenant>();

            IQueryable<TenantModel> query = _dbContext.Tenants;

            query = query.Where(tenant => tenant.DeveloperUuid == developerUuid);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(tenant => EF.Functions.ILike(tenant.Name, $"{searchTerm}%"));
            }

            if (sortOrderEnum == SortOrder.Asc)
            {
                switch (sortByEnum)
                {
                    case SortByTenant.Name:
                        query = query.OrderBy(tenant => tenant.Name);

                        break;
                    case SortByTenant.Created_at:
                        query = query
                            .OrderBy(tenant => tenant.Name)
                            .OrderBy(tenant => tenant.CreatedAt);

                        break;
                    case SortByTenant.Last_updated_at:
                        query = query
                            .OrderBy(tenant => tenant.Name)
                            .OrderBy(tenant => tenant.LastUpdatedAt);

                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (sortByEnum)
                {
                    case SortByTenant.Name:
                        query = query.OrderByDescending(tenant => tenant.Name);

                        break;
                    case SortByTenant.Created_at:
                        query = query
                            .OrderBy(tenant => tenant.Name)
                            .OrderByDescending(tenant => tenant.CreatedAt);

                        break;
                    case SortByTenant.Last_updated_at:
                        query = query
                            .OrderBy(tenant => tenant.Name)
                            .OrderByDescending(tenant => tenant.LastUpdatedAt);

                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            pagingResult.TotalCount = await query.CountAsync().ConfigureAwait(false);

            query = query.Skip(offsetInt);
            query = query.Take(limitInt);

            var tenantModels = await query.ToListAsync().ConfigureAwait(false);

            pagingResult.Result = tenantModels
                .Select(tenantModel =>
                {
                    var tenant = new Tenant()
                    {
                        Uuid = Convert.ToString(tenantModel.Uuid),
                        Name = tenantModel.Name,
                        Subdomain = tenantModel.SubdomainPatterns[0],
                        WebUrl = tenantModel.WebUrl,
                        EcbBackendApiUrl = tenantModel.EcbBackendApiUrl,
                        PortalsBackendApiUrl = tenantModel.PortalsBackendApiUrl,
                        FrontendApiUrl = tenantModel.FrontendApiUrl
                    };

                    string logoSvgUrl = tenantModel.LogoSvgUrl;
                    if (string.IsNullOrEmpty(logoSvgUrl))
                        logoSvgUrl = developerInfo.LogoSvgUrl;

                    tenant.LogoSvgUrl = logoSvgUrl;

                    string logoPngUrl = tenantModel.LogoPngUrl;
                    if (string.IsNullOrEmpty(logoPngUrl))
                        logoPngUrl = developerInfo.LogoPngUrl;

                    tenant.LogoPngUrl = logoPngUrl;

                    string iconIcoUrl = tenantModel.IconIcoUrl;
                    if (string.IsNullOrEmpty(iconIcoUrl))
                        iconIcoUrl = developerInfo.IconIcoUrl;

                    tenant.IconIcoUrl = iconIcoUrl;

                    string appleTouchIconUrl = tenantModel.AppleTouchIconUrl;
                    if (string.IsNullOrEmpty(appleTouchIconUrl))
                        appleTouchIconUrl = developerInfo.AppleTouchIconUrl;

                    tenant.AppleTouchIconUrl = appleTouchIconUrl;

                    string customCss = tenantModel.CustomCss;
                    if (string.IsNullOrEmpty(customCss))
                        customCss = developerInfo.CustomCss;

                    tenant.CustomCss = customCss;

                    int? primaryColor = tenantModel.PrimaryColor;
                    if (primaryColor == null)
                        primaryColor = developerInfo.PrimaryColor;

                    tenant.PrimaryColor = primaryColor;

                    int? secondaryColor = tenantModel.SecondaryColor;
                    if (secondaryColor == null)
                        secondaryColor = developerInfo.SecondaryColor;

                    tenant.SecondaryColor = secondaryColor;

                    int? textOnPrimaryColor = tenantModel.TextOnPrimaryColor;
                    if (textOnPrimaryColor == null)
                        textOnPrimaryColor = developerInfo.TextOnPrimaryColor;

                    tenant.TextOnPrimaryColor = textOnPrimaryColor;

                    int? textOnSecondaryColor = tenantModel.TextOnSecondaryColor;
                    if (textOnSecondaryColor == null)
                        textOnSecondaryColor = developerInfo.TextOnSecondaryColor;

                    tenant.TextOnSecondaryColor = textOnSecondaryColor;

                    string supportEmail = tenantModel.SupportEmail;
                    if (string.IsNullOrEmpty(supportEmail))
                        supportEmail = developerInfo.SupportEmail;

                    tenant.SupportEmail = supportEmail;

                    string supportEmailDisplayName = tenantModel.SupportEmailDisplayName;
                    if (string.IsNullOrEmpty(supportEmailDisplayName))
                        supportEmailDisplayName = developerInfo.SupportEmailDisplayName;

                    tenant.SupportEmailDisplayName = supportEmailDisplayName;

                    string noreplyEmail = tenantModel.NoreplyEmail;
                    if (string.IsNullOrEmpty(noreplyEmail))
                        noreplyEmail = developerInfo.NoreplyEmail;

                    tenant.NoreplyEmail = noreplyEmail;

                    string noreplyEmailDisplayName = tenantModel.NoreplyEmailDisplayName;
                    if (string.IsNullOrEmpty(noreplyEmailDisplayName))
                        noreplyEmailDisplayName = developerInfo.NoreplyEmailDisplayName;

                    tenant.NoreplyEmailDisplayName = noreplyEmailDisplayName;

                    string productName = isPortals ? tenantModel.PortalsProductName : tenantModel.EcbProductName;
                    if (string.IsNullOrEmpty(productName))
                        productName = isPortals ? developerInfo.PortalsProductName : developerInfo.EcbProductName;

                    tenant.ProductName = productName;

                    string defaultCulture = tenantModel.DefaultCulture;
                    if (string.IsNullOrEmpty(defaultCulture))
                        defaultCulture = "en";

                    tenant.DefaultCulture = defaultCulture;

                    string defaultCurrency = tenantModel.DefaultCurrency;
                    if (string.IsNullOrEmpty(defaultCurrency))
                        defaultCurrency = "eur";

                    tenant.DefaultCurrency = defaultCurrency;

                    tenant.CustomTenantSettingsJson = tenantModel.CustomTenantSettingsJson;

                    tenant.UsersAreExternallyManaged = !string.IsNullOrEmpty(tenantModel.ExternalAuthenticationMethod);

                    tenant.ExternalUsersAreManuallyManaged = tenantModel.ExternalUsersAreManuallyManaged;

                    tenant.CreatedByUserUuid = tenantModel.CreatedByUserUuid;

                    tenant.Version = tenantModel.Version;
                    
                    tenant.CreatedAt = tenantModel.CreatedAt;
                    tenant.LastUpdatedAt = tenantModel.LastUpdatedAt;

                    return tenant;
                })
                .ToList();

            return pagingResult;
        }

        public static string ProcessName(string name)
        {
            name = name?.Trim();

            if (string.IsNullOrEmpty(name))
                throw new RequestFailedApiException(RequestFailedApiException.NameMissing, "The name is missing");

            if (!ApiImpl.SafeString.IsMatch(name))
                throw new RequestFailedApiException(RequestFailedApiException.NameInvalid, "The name is invalid");

            if (name.Length > TenantModel.MaxNameLength)
                throw new RequestFailedApiException(RequestFailedApiException.NameTooLong, "The name is too long");

            return name;
        }

        private string ProcessSubdomain(string subdomain)
        {
            subdomain = subdomain?.Trim();

            if (string.IsNullOrEmpty(subdomain))
                throw new RequestFailedApiException(RequestFailedApiException.SubdomainMissing, "The subdomain is missing");

            subdomain = subdomain.ToLower();

            if (!Tenant.IsMatch(subdomain))
                throw new RequestFailedApiException(RequestFailedApiException.SubdomainInvalid, "The subdomain is invalid");

            if (subdomain.Length > TenantModel.MaxSubdomainPatternLength)
                throw new RequestFailedApiException(RequestFailedApiException.SubdomainTooLong, "The subdomain is too long");

            return subdomain.ToLower();
        }

        private string ProcessLogoSvgUrl(string logoSvgUrl)
        {
            logoSvgUrl = logoSvgUrl?.Trim();

            if (string.IsNullOrEmpty(logoSvgUrl))
                throw new RequestFailedApiException(RequestFailedApiException.LogoSvgUrlMissing, "The logo SVG URL is missing");

            if (logoSvgUrl.Length > TenantModel.MaxUrlLength)
                throw new RequestFailedApiException(RequestFailedApiException.LogoSvgUrlTooLong, "The logo SVG URL is too long");

            return logoSvgUrl;
        }

        private string ProcessLogoPngUrl(string logoPngUrl)
        {
            logoPngUrl = logoPngUrl?.Trim();

            if (string.IsNullOrEmpty(logoPngUrl))
                throw new RequestFailedApiException(RequestFailedApiException.LogoPngUrlMissing, "The logo PNG URL is missing");

            if (logoPngUrl.Length > TenantModel.MaxUrlLength)
                throw new RequestFailedApiException(RequestFailedApiException.LogoPngUrlTooLong, "The logo PNG URL is too long");

            return logoPngUrl;
        }

        private string ProcessIconIcoUrl(string iconIcoUrl)
        {
            iconIcoUrl = iconIcoUrl?.Trim();

            if (string.IsNullOrEmpty(iconIcoUrl))
                throw new RequestFailedApiException(RequestFailedApiException.IconIcoUrlMissing, "The icon ICO URL is missing");

            if (iconIcoUrl.Length > TenantModel.MaxUrlLength)
                throw new RequestFailedApiException(RequestFailedApiException.IconIcoUrlTooLong, "The icon ICO URL is too long");

            return iconIcoUrl;
        }

        private string ProcessAppleTouchIconUrl(string appleTouchIconUrl)
        {
            appleTouchIconUrl = appleTouchIconUrl?.Trim();

            if (string.IsNullOrEmpty(appleTouchIconUrl))
                throw new RequestFailedApiException(RequestFailedApiException.AppleTouchIconUrlMissing, "The apple touch icon URL is missing");

            if (appleTouchIconUrl.Length > TenantModel.MaxUrlLength)
                throw new RequestFailedApiException(RequestFailedApiException.AppleTouchIconUrlTooLong, "The apple touch icon URL is too long");

            return appleTouchIconUrl;
        }

        public async static Task<string> ProcessCustomCssAsync(string customCss, bool optimizeCustomCss)
        {
            customCss = customCss?.Trim();

            if (string.IsNullOrEmpty(customCss))
                return null;

            try
            {
                var parser = new StylesheetParser(includeUnknownRules: true, includeUnknownDeclarations: true, tolerateInvalidSelectors: true, tolerateInvalidValues: true, tolerateInvalidConstraints: true, preserveComments: true, preserveDuplicateProperties: true);

                if (optimizeCustomCss)
                {
                    // check if custom CSS is valid

                    var stylesheet = await parser.ParseAsync(customCss).ConfigureAwait(false);

                    // return optimized

                    var stringWriter = new StringWriter();
                    stylesheet.ToCss(stringWriter, new CompressedStyleFormatter());

                    return stringWriter.ToString();
                }
                else
                {
                    // check if custom CSS is valid

                    await parser.ParseAsync(customCss).ConfigureAwait(false);

                    // return non-optimized

                    return customCss;
                }
            }
            catch (Exception e)
            {
                throw new RequestFailedApiException(RequestFailedApiException.CustomCssInvalid, $"The custom CSS is invalid: {e.Message}", e.Message, name: null);
            }
        }

        private int? ProcessPrimaryColor(int? primaryColor)
        {
            if (primaryColor == null)
                return null;

            try
            {
                return Convert.ToInt32(primaryColor);
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.PrimaryColorInvalid, "The primary color is invalid");
            }
        }

        private int? ProcessSecondaryColor(int? secondaryColor)
        {
            if (secondaryColor == null)
                return null;

            try
            {
                return Convert.ToInt32(secondaryColor);
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.SecondaryColorInvalid, "The secondary color is invalid");
            }
        }

        private int? ProcessTextOnPrimaryColor(int? textOnPrimaryColor)
        {
            if (textOnPrimaryColor == null)
                return null;

            try
            {
                return Convert.ToInt32(textOnPrimaryColor);
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.TextOnPrimaryColorInvalid, "The text on primary color is invalid");
            }
        }

        private int? ProcessTextOnSecondaryColor(int? textOnSecondaryColor)
        {
            if (textOnSecondaryColor == null)
                return null;

            try
            {
                return Convert.ToInt32(textOnSecondaryColor);
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.TextOnSecondaryColorInvalid, "The text on secondary color is invalid");
            }
        }

        public static string ProcessSupportEmail(string supportEmail)
        {
            supportEmail = supportEmail?.Trim();

            if (string.IsNullOrEmpty(supportEmail))
                return null;

            if (!SafeString.IsMatch(supportEmail))
                throw new RequestFailedApiException(RequestFailedApiException.SupportEmailInvalid, $"The support email address is invalid");

            if (supportEmail.Length > MaxEmailAddressLength)
                throw new RequestFailedApiException(RequestFailedApiException.SupportEmailTooLong, $"The support email address is too long");

            return supportEmail;
        }

        public static string ProcessSupportReplyToEmail(string supportReplyToEmail)
        {
            supportReplyToEmail = supportReplyToEmail?.Trim();

            if (string.IsNullOrEmpty(supportReplyToEmail))
                return null;

            if (!SafeString.IsMatch(supportReplyToEmail))
                throw new RequestFailedApiException(RequestFailedApiException.SupportReplyToEmailInvalid, $"The support reply email address is invalid");

            if (supportReplyToEmail.Length > MaxEmailAddressLength)
                throw new RequestFailedApiException(RequestFailedApiException.SupportReplyToEmailTooLong, $"The support reply email address is too long");

            return supportReplyToEmail;
        }

        public static string ProcessSupportEmailDisplayName(string supportEmailDisplayName)
        {
            supportEmailDisplayName = supportEmailDisplayName?.Trim();

            if (string.IsNullOrEmpty(supportEmailDisplayName))
                return null;

            if (!SafeString.IsMatch(supportEmailDisplayName))
                throw new RequestFailedApiException(RequestFailedApiException.SupportEmailDisplayNameInvalid, $"The support email display name is invalid");

            if (supportEmailDisplayName.Length > MaxEmailDisplayNameLength)
                throw new RequestFailedApiException(RequestFailedApiException.SupportEmailDisplayNameTooLong, $"The support email display name is too long");

            return supportEmailDisplayName;
        }

        public static string ProcessNoreplyEmail(string noreplyEmail)
        {
            noreplyEmail = noreplyEmail?.Trim();

            if (string.IsNullOrEmpty(noreplyEmail))
                return null;

            if (!SafeString.IsMatch(noreplyEmail))
                throw new RequestFailedApiException(RequestFailedApiException.NoreplyEmailInvalid, $"The generic email address is invalid");

            if (noreplyEmail.Length > MaxEmailAddressLength)
                throw new RequestFailedApiException(RequestFailedApiException.NoreplyEmailTooLong, $"The generic email address is too long");

            return noreplyEmail;
        }

        public static string ProcessNoreplyReplyToEmail(string noreplyReplyToEmail)
        {
            noreplyReplyToEmail = noreplyReplyToEmail?.Trim();

            if (string.IsNullOrEmpty(noreplyReplyToEmail))
                return null;

            if (!SafeString.IsMatch(noreplyReplyToEmail))
                throw new RequestFailedApiException(RequestFailedApiException.NoreplyEmailInvalid, $"The generic reply email address is invalid");

            if (noreplyReplyToEmail.Length > MaxEmailAddressLength)
                throw new RequestFailedApiException(RequestFailedApiException.NoreplyEmailTooLong, $"The generic reply email address is too long");

            return noreplyReplyToEmail;
        }

        public static string ProcessNoreplyEmailDisplayName(string noreplyEmailDisplayName)
        {
            noreplyEmailDisplayName = noreplyEmailDisplayName?.Trim();

            if (string.IsNullOrEmpty(noreplyEmailDisplayName))
                return null;

            if (!SafeString.IsMatch(noreplyEmailDisplayName))
                throw new RequestFailedApiException(RequestFailedApiException.NoreplyEmailDisplayNameInvalid, $"The generic email display name is invalid");

            if (noreplyEmailDisplayName.Length > MaxEmailDisplayNameLength)
                throw new RequestFailedApiException(RequestFailedApiException.NoreplyEmailDisplayNameTooLong, $"The generic email display name is too long");

            return noreplyEmailDisplayName;
        }

        private string ProcessDefaultCulture(string defaultCulture)
        {
            if (string.IsNullOrEmpty(defaultCulture))
                return null;

            try
            {
                var cultureInfo = CultureInfo.GetCultureInfo(defaultCulture);

                return cultureInfo.TwoLetterISOLanguageName;
            }
            catch (Exception)
            {
                throw new RequestFailedApiException(RequestFailedApiException.DefaultCultureInvalid, "The default culture is invalid");
            }
        }

        private string ProcessDefaultCurrency(string defaultCurrency)
        {
            if (string.IsNullOrEmpty(defaultCurrency))
                return null;

            if (string.Equals(defaultCurrency, "eur"))
                return defaultCurrency;
            else if (string.Equals(defaultCurrency, "usd"))
                return defaultCurrency;
            
            throw new RequestFailedApiException(RequestFailedApiException.DefaultCurrencyInvalid, "The default currency is invalid");
        }

        private static bool ProcessEnableAudit(bool? enableAudit)
        {
            return enableAudit ?? false;
        }

        private enum SortByTenant
        {
            Name,
            Created_at,
            Last_updated_at
        }

        private SortByTenant ProcessSortByTenant(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy))
                return SortByTenant.Name;

            if (string.Equals(sortBy, "name"))
                return SortByTenant.Name;
            else if (string.Equals(sortBy, "created_at"))
                return SortByTenant.Created_at;
            else if (string.Equals(sortBy, "last_updated_at"))
                return SortByTenant.Last_updated_at;
            else
                throw new RequestFailedApiException(RequestFailedApiException.SortByInvalid, "The sort by value is invalid");
        }

        private enum SortOrder
        {
            Asc,
            Desc
        }

        private SortOrder ProcessSortOrder(string sortOrder)
        {
            if (string.IsNullOrEmpty(sortOrder))
                return SortOrder.Asc;

            if (string.Equals(sortOrder, "asc"))
                return SortOrder.Asc;
            else if (string.Equals(sortOrder, "desc"))
                return SortOrder.Desc;
            else
                throw new RequestFailedApiException(RequestFailedApiException.SortOrderInvalid, "The sort order is invalid");
        }
    }
}
