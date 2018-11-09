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

        private readonly Dictionary<long, DeveloperWrapper> _developerMappingsByUuid;

        public List<IDeveloperInfo> Developers { get; internal set; }
        public List<ITenantInfo> Tenants { get; internal set; }

        private readonly ILogger<TenantDataProviderImpl> _logger;

        private class DeveloperWrapper
        {
            private Dictionary<string, ITenantInfo> _tenantInfoMappings;
            private Dictionary<long, ITenantInfo> _tenantInfoMappingsByUuid;

            public List<ITenantInfo> TenantInfos { get; set; }
            
            public DeveloperModel Developer { get; private set; }

            public DeveloperWrapper(DeveloperModel developer)
            {
                Developer = developer;

                _tenantInfoMappings = new Dictionary<string, ITenantInfo>();
                _tenantInfoMappingsByUuid = new Dictionary<long, ITenantInfo>();

                TenantInfos = new List<ITenantInfo>();

                developer.Tenants.ForEach(tenant =>
                {
                    string subdomainPattern = tenant.SubdomainPattern;

                    if (developer.Certificate != null && developer.Certificate.Length > 0 &&
                        string.IsNullOrEmpty(developer.CertificatePassword))
                    {
                        throw new Exception("Developer in tenant database has certificate set, but no certificate password is present");
                    }

                    if (string.IsNullOrEmpty(tenant.Name))
                        throw new Exception("The tenant name is empty");

                    if (string.IsNullOrEmpty(tenant.ApiUrl))
                        throw new Exception("The tenant API url is empty");

                    if (string.IsNullOrEmpty(tenant.WebUrl))
                        throw new Exception("The tenant web url is empty");

                    string logoUrl = tenant.LogoUrl;
                    if (string.IsNullOrEmpty(logoUrl))
                        logoUrl = developer.LogoUrl;

                    int? primaryColor = tenant.PrimaryColor;
                    if (primaryColor == null)
                        primaryColor = developer.PrimaryColor;

                    int? secondaryColor = tenant.SecondaryColor;
                    if (secondaryColor == null)
                        secondaryColor = developer.SecondaryColor;

                    int? textOnPrimaryColor = tenant.TextOnPrimaryColor;
                    if (textOnPrimaryColor == null)
                        textOnPrimaryColor = developer.TextOnPrimaryColor;

                    int? textOnSecondaryColor = tenant.TextOnSecondaryColor;
                    if (textOnSecondaryColor == null)
                        textOnSecondaryColor = developer.TextOnSecondaryColor;

                    var tenantInfo = new TenantInfoImpl()
                    {
                        DeveloperUuid = developer.Uuid,
                        DeveloperAuthority = developer.Authority,
                        DeveloperAudience = developer.Audience,
                        DeveloperCertificate = developer.Certificate,
                        DeveloperCertificatePassword = developer.CertificatePassword,
                        DeveloperAuthCookieDomain = developer.AuthCookieDomain,
                        TenantUuid = tenant.Uuid,
                        Name = tenant.Name,
                        LogoUrl = logoUrl,
                        PrimaryColor = (int)primaryColor,
                        SecondaryColor = (int)secondaryColor,
                        TextOnPrimaryColor = (int)textOnPrimaryColor,
                        TextOnSecondaryColor = (int)textOnSecondaryColor,
                        ApiUrl = tenant.ApiUrl,
                        WebUrl = tenant.WebUrl
                    };

                    string[] subdomainPatternParts = subdomainPattern.Split(';');

                    subdomainPatternParts.ToList().ForEach(subdomainPatternPart =>
                    {
                        _tenantInfoMappings.Add(subdomainPatternPart, tenantInfo);                        
                    });

                    _tenantInfoMappingsByUuid.Add(tenantInfo.TenantUuid, tenantInfo);

                    TenantInfos.Add(tenantInfo);
                });
            }

            internal ITenantInfo LookupTenantBySubDomain(string subDomainLookup)
            {
                if (!_tenantInfoMappings.ContainsKey(subDomainLookup))
                    return null;

                var tenantInfo = _tenantInfoMappings[subDomainLookup];

                return tenantInfo;
            }

            internal ITenantInfo LookupTenantByUuid(long tenantUuid)
            {
                if (!_tenantInfoMappingsByUuid.ContainsKey(tenantUuid))
                    return null;

                return _tenantInfoMappingsByUuid[tenantUuid];
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
                    _developerMappingsByUuid = new Dictionary<long, DeveloperWrapper>();

                    Developers = new List<IDeveloperInfo>();
                    Tenants = new List<ITenantInfo>();

                    developers.ForEach(developer =>
                    {
                        var developerWrapper = new DeveloperWrapper(developer);

                        _developerMappings.Add(developer.HostPattern, developerWrapper);
                        _developerMappingsByUuid.Add(developer.Uuid, developerWrapper);

                        Tenants.AddRange(developerWrapper.TenantInfos);

                        if (string.IsNullOrEmpty(developer.Authority))
                            throw new Exception("The developer authority is empty");

                        if (string.IsNullOrEmpty(developer.Audience))
                            throw new Exception("The developer audience is empty");

                        if (string.IsNullOrEmpty(developer.AuthCookieDomain))
                            throw new Exception("The developer auth cookie domain is empty");

                        if (string.IsNullOrEmpty(developer.Name))
                            throw new Exception("The developer name is empty");

                        if (string.IsNullOrEmpty(developer.LogoUrl))
                            throw new Exception("The developer logo URL is empty");
                       
                        var developerInfo = new DeveloperInfoImpl()
                        {
                            DeveloperUuid = developer.Uuid,
                            Authority = developer.Authority,
                            Audience = developer.Audience,
                            Certificate = developer.Certificate,
                            CertificatePassword = developer.CertificatePassword,
                            AuthCookieDomain = developer.AuthCookieDomain,
                            Name = developer.Name,
                            LogoUrl = developer.LogoUrl,
                            PrimaryColor = developer.PrimaryColor,
                            SecondaryColor = developer.SecondaryColor,
                            TextOnPrimaryColor = developer.TextOnPrimaryColor,
                            TextOnSecondaryColor = developer.TextOnSecondaryColor
                        };

                        Developers.Add(developerInfo);
                    });
                }
            }

            _logger = logger;
        }

        public ITenantInfo LookupTenantByHost(string host)
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

            var tenantInfo = developerWrapper.LookupTenantBySubDomain(subDomainLookup);
            if (tenantInfo == null)
            {
                _logger.LogError($"No developer found for host {host}, developer {developerWrapper.Developer.Uuid} and sub domain lookup {subDomainLookup}");

                return null;
            }

            return tenantInfo;            
        }

        public ITenantInfo LookupTenantByUuid(long developerUuid, long tenantUuid)
        {
            if (!_developerMappingsByUuid.ContainsKey(developerUuid))
                return null;

            return _developerMappingsByUuid[developerUuid].LookupTenantByUuid(tenantUuid);
        }
    }
}
