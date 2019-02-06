using HCore.Storage;
using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Database.SqlServer.Models.Impl;
using HCore.Tenants.Models;
using HCore.Tenants.Models.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HCore.Tenants.Providers.Impl
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

                    if (string.IsNullOrEmpty(tenant.FrontendApiUrl))
                        throw new Exception("The tenant frontend API URL is empty");

                    if (string.IsNullOrEmpty(tenant.BackendApiUrl))
                        throw new Exception("The tenant backend API url is empty");

                    if (string.IsNullOrEmpty(tenant.WebUrl))
                        throw new Exception("The tenant web url is empty");

                    string logoSvgUrl = tenant.LogoSvgUrl;
                    if (string.IsNullOrEmpty(logoSvgUrl))
                        logoSvgUrl = developer.LogoSvgUrl;

                    string logoPngUrl = tenant.LogoPngUrl;
                    if (string.IsNullOrEmpty(logoPngUrl))
                        logoPngUrl = developer.LogoPngUrl;

                    string iconIcoUrl = tenant.IconIcoUrl;
                    if (string.IsNullOrEmpty(iconIcoUrl))
                        iconIcoUrl = developer.IconIcoUrl;

                    string storageImplementation = tenant.StorageImplementation;
                    if (string.IsNullOrEmpty(storageImplementation))
                        storageImplementation = developer.StorageImplementation;

                    // storage is optional

                    string storageConnectionString = null;

                    if (!string.IsNullOrEmpty(storageImplementation))
                    {
                        if (!storageImplementation.Equals(StorageConstants.StorageImplementationAzure) && !storageImplementation.Equals(StorageConstants.StorageImplementationGoogleCloud))
                            throw new Exception("The tenant storage implementation specification is invalid");

                        storageConnectionString = tenant.StorageConnectionString;
                        if (string.IsNullOrEmpty(storageConnectionString))
                            storageConnectionString = developer.StorageConnectionString;
                    }

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

                    string supportEmail = tenant.SupportEmail;
                    if (string.IsNullOrEmpty(supportEmail))
                        supportEmail = developer.SupportEmail;

                    string noreplyEmail = tenant.NoreplyEmail;
                    if (string.IsNullOrEmpty(noreplyEmail))
                        noreplyEmail = developer.NoreplyEmail;

                    string productName = tenant.ProductName;
                    if (string.IsNullOrEmpty(productName))
                        productName = developer.ProductName;

                    var tenantInfo = new TenantInfoImpl()
                    {
                        DeveloperUuid = developer.Uuid,
                        DeveloperAuthority = developer.Authority,
                        DeveloperAudience = developer.Audience,
                        DeveloperCertificate = developer.Certificate,
                        DeveloperCertificatePassword = developer.CertificatePassword,
                        DeveloperAuthCookieDomain = developer.AuthCookieDomain,
                        DeveloperPrivacyPolicyUrl = developer.PrivacyPolicyUrl,
                        DeveloperPrivacyPolicyVersion = developer.PrivacyPolicyVersion,
                        DeveloperTermsAndConditionsUrl = developer.TermsAndConditionsUrl,
                        DeveloperTermsAndConditionsVersion = developer.TermsAndConditionsVersion,
                        TenantUuid = tenant.Uuid,
                        Name = tenant.Name,
                        LogoSvgUrl = logoSvgUrl,
                        LogoPngUrl = logoPngUrl,
                        IconIcoUrl = iconIcoUrl,
                        StorageImplementation = storageImplementation,
                        StorageConnectionString = storageConnectionString,
                        PrimaryColor = (int)primaryColor,
                        SecondaryColor = (int)secondaryColor,
                        TextOnPrimaryColor = (int)textOnPrimaryColor,
                        TextOnSecondaryColor = (int)textOnSecondaryColor,
                        PrimaryColorHex = ConvertToHexColor(primaryColor),
                        SecondaryColorHex = ConvertToHexColor(secondaryColor),
                        TextOnPrimaryColorHex = ConvertToHexColor(textOnPrimaryColor),
                        TextOnSecondaryColorHex = ConvertToHexColor(textOnSecondaryColor),
                        SupportEmail = supportEmail,
                        NoreplyEmail = noreplyEmail,
                        ProductName = productName,
                        BackendApiUrl = tenant.BackendApiUrl,
                        FrontendApiUrl = tenant.FrontendApiUrl,
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

            private string ConvertToHexColor(int? color)
            {
                return "#" + (color != null ? ((int)color).ToString("X6") : "000000");
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

                        if (string.IsNullOrEmpty(developer.LogoSvgUrl))
                            throw new Exception("The developer logo SVG URL is empty");

                        if (string.IsNullOrEmpty(developer.LogoPngUrl))
                            throw new Exception("The developer logo PNG URL is empty");

                        if (string.IsNullOrEmpty(developer.IconIcoUrl))
                            throw new Exception("The developer icon ICO URL is empty");

                        // storage is optional
                        
                        string storageImplementation = developer.StorageImplementation;
                        if (!string.IsNullOrEmpty(storageImplementation))
                        {
                            if (!storageImplementation.Equals(StorageConstants.StorageImplementationAzure) && !storageImplementation.Equals(StorageConstants.StorageImplementationGoogleCloud))
                                throw new Exception("The developer storage implementation specification is invalid");

                            if (string.IsNullOrEmpty(developer.StorageConnectionString))
                                throw new Exception("The developer storage connection string is empty");
                        }

                        if (string.IsNullOrEmpty(developer.SupportEmail))
                            throw new Exception("The developer support email is empty");

                        if (string.IsNullOrEmpty(developer.NoreplyEmail))
                            throw new Exception("The developer noreply email is empty");

                        if (string.IsNullOrEmpty(developer.ProductName))
                            throw new Exception("The developer product name is empty");

                        var developerInfo = new DeveloperInfoImpl()
                        {
                            DeveloperUuid = developer.Uuid,
                            Authority = developer.Authority,
                            Audience = developer.Audience,
                            Certificate = developer.Certificate,
                            CertificatePassword = developer.CertificatePassword,
                            AuthCookieDomain = developer.AuthCookieDomain,
                            Name = developer.Name,
                            LogoSvgUrl = developer.LogoSvgUrl,
                            LogoPngUrl = developer.LogoPngUrl,
                            IconIcoUrl = developer.IconIcoUrl,
                            StorageImplementation = developer.StorageImplementation,
                            StorageConnectionString = developer.StorageConnectionString,
                            PrimaryColor = developer.PrimaryColor,
                            SecondaryColor = developer.SecondaryColor,
                            TextOnPrimaryColor = developer.TextOnPrimaryColor,
                            TextOnSecondaryColor = developer.TextOnSecondaryColor,
                            SupportEmail = developer.SupportEmail,
                            NoreplyEmail = developer.NoreplyEmail,
                            ProductName = developer.ProductName
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
