using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Database.SqlServer.Models.Impl;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HCore.Tenants.Impl
{
    internal class TenantDataProviderImpl : ITenantDataProvider
    {
        private readonly Dictionary<string, DeveloperWrapper> _developerMappings;

        public List<ITenantInfo> Tenants { get; internal set; }

        private readonly ILogger<TenantDataProviderImpl> _logger;

        private class DeveloperWrapper
        {
            private Dictionary<string, ITenantInfo> _tenantInfoMappings;

            public List<ITenantInfo> TenantInfos { get; set; }
            
            public DeveloperModel Developer { get; private set; }

            public DeveloperWrapper(DeveloperModel developer)
            {
                Developer = developer;

                _tenantInfoMappings = new Dictionary<string, ITenantInfo>();
                TenantInfos = new List<ITenantInfo>();

                developer.Tenants.ForEach(tenant =>
                {
                    string subdomainPattern = tenant.SubdomainPattern;

                    var tenantInfo = new TenantInfoImpl()
                    {
                        DeveloperUuid = developer.Uuid,
                        DeveloperAuthority = developer.Authority,
                        DeveloperCertificate = developer.Certificate,
                        DeveloperAuthCookieDomain = developer.AuthCookieDomain,
                        TenantUuid = tenant.Uuid,
                        Name = tenant.Name,
                        LogoUrl = tenant.LogoUrl
                    };

                    string[] subdomainPatternParts = subdomainPattern.Split(';');

                    subdomainPatternParts.ToList().ForEach(subdomainPatternPart =>
                    {
                        _tenantInfoMappings.Add(subdomainPatternPart, tenantInfo);
                        
                    });

                    TenantInfos.Add(tenantInfo);
                });
            }

            internal ITenantInfo LookupTenant(string subDomainLookup)
            {
                if (!_tenantInfoMappings.ContainsKey(subDomainLookup))
                    return null;

                var tenantInfo = _tenantInfoMappings[subDomainLookup];

                return tenantInfo;
            }
        }

        public TenantDataProviderImpl(IServiceProvider serviceProvider, ILogger<TenantDataProviderImpl> logger)
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                using (var dbContext = scope.ServiceProvider.GetRequiredService<SqlServerTenantDbContext>())
                {
                    IQueryable<DeveloperModel> query = dbContext.Developers;

                    query = query.Include(developerModel => developerModel.Tenants);

                    List<DeveloperModel> developers = query.ToList();

                    _developerMappings = new Dictionary<string, DeveloperWrapper>();
                    Tenants = new List<ITenantInfo>();

                    developers.ForEach(developer =>
                    {
                        var developerWrapper = new DeveloperWrapper(developer);

                        _developerMappings.Add(developer.HostPattern, developerWrapper);

                        Tenants.AddRange(developerWrapper.TenantInfos);                        
                    });
                }
            }

            _logger = logger;
        }

        public ITenantInfo LookupTenant(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                _logger.LogError($"Host is empty for tenant parsing");

                return null;
            }
            
            string[] parts = host.Split('.');

            if (parts.Length == 2)
            {
                // prefix with www

                host = "www." + host;
            }

            parts = host.Split('.');

            if (parts.Length < 3)
            {
                _logger.LogError($"Host {host} does not have enough parts for tenant parsing");

                return null;
            }

            if (parts.Length > 3)
            {
                _logger.LogError($"Host {host} has too many parts for tenant parsing");

                return null;
            }

            if (string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]) || string.IsNullOrEmpty(parts[2]))
            {
                _logger.LogError($"Host {host} has empty parts which is not allowed for tenant parsing");

                return null;
            }

            string subDomainLookup = parts[0];
            string hostLookup = parts[1] + "." + parts[2];

            if (!_developerMappings.ContainsKey(hostLookup))
            {
                _logger.LogError($"No developer found for host {host}");

                return null;
            }

            var developerWrapper = _developerMappings[hostLookup];
            if (developerWrapper == null)
            {
                _logger.LogError($"No developer found for host {host}");

                return null;
            }

            var tenantInfo = developerWrapper.LookupTenant(subDomainLookup);
            if (tenantInfo == null)
            {
                _logger.LogError($"No developer found for host {host}, developer {developerWrapper.Developer.Uuid} and sub domain lookup {subDomainLookup}");

                return null;
            }

            return tenantInfo;            
        }
    }
}
