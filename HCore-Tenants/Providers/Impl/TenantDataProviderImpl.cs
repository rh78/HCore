using HCore.Directory;
using HCore.Storage;
using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Database.SqlServer.Models.Impl;
using HCore.Tenants.Models;
using HCore.Tenants.Models.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace HCore.Tenants.Providers.Impl
{
    internal class TenantDataProviderImpl : ITenantDataProvider
    {
        private readonly Dictionary<string, DeveloperWrapper> _developerMappings;

        private readonly Dictionary<long, DeveloperWrapper> _developerMappingsByUuid;

        public List<IDeveloperInfo> Developers { get; internal set; }
        public List<ITenantInfo> Tenants { get; internal set; }

        public int? HealthCheckPort { get; internal set; }
        public string HealthCheckTenantHost { get; internal set; }

        private readonly ILogger<TenantDataProviderImpl> _logger;

        private class DeveloperWrapper
        {
            private Dictionary<string, ITenantInfo> _tenantInfoMappings;
            private Dictionary<long, ITenantInfo> _tenantInfoMappingsByUuid;

            public List<ITenantInfo> TenantInfos { get; set; }
            
            public DeveloperModel Developer { get; private set; }

            public DeveloperWrapper(DeveloperModel developer, X509Certificate2 standardSamlCertificate)
            {
                Developer = developer;

                _tenantInfoMappings = new Dictionary<string, ITenantInfo>();
                _tenantInfoMappingsByUuid = new Dictionary<long, ITenantInfo>();

                TenantInfos = new List<ITenantInfo>();

                developer.Tenants.ForEach(tenant =>
                {
                    string subdomainPattern = tenant.SubdomainPattern;

                    X509Certificate2 developerCertificate = null;

                    if (!string.IsNullOrEmpty(developer.Certificate))
                    {
                        if (string.IsNullOrEmpty(developer.CertificatePassword))
                            throw new Exception("Developer in tenant database has certificate set, but no certificate password is present");

                        developerCertificate = new X509Certificate2(GetCertificateBytesFromPEM(developer.Certificate), developer.CertificatePassword);
                    }

                    if (string.IsNullOrEmpty(tenant.Name))
                        throw new Exception("The tenant name is empty");

                    if (string.IsNullOrEmpty(tenant.FrontendApiUrl))
                        throw new Exception("The tenant frontend API URL is empty");

                    if (string.IsNullOrEmpty(tenant.BackendApiUrl))
                        throw new Exception("The tenant backend API url is empty");

                    if (string.IsNullOrEmpty(tenant.WebUrl))
                        throw new Exception("The tenant web url is empty");

                    bool? requiresTermsAndConditions = tenant.RequiresTermsAndConditions;
                    if (requiresTermsAndConditions == null)
                        requiresTermsAndConditions = developer.RequiresTermsAndConditions;

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

                    string customInvitationEmailTextPrefix = tenant.CustomInvitationEmailTextPrefix;
                    if (string.IsNullOrEmpty(customInvitationEmailTextPrefix))
                        customInvitationEmailTextPrefix = null;

                    string customInvitationEmailTextSuffix = tenant.CustomInvitationEmailTextSuffix;
                    if (string.IsNullOrEmpty(customInvitationEmailTextSuffix))
                        customInvitationEmailTextSuffix = null;

                    string productName = tenant.ProductName;
                    if (string.IsNullOrEmpty(productName))
                        productName = developer.ProductName;

                    string defaultCulture = tenant.DefaultCulture;
                    if (string.IsNullOrEmpty(defaultCulture))
                        defaultCulture = "en";

                    string defaultCurrency = tenant.DefaultCurrency;
                    if (string.IsNullOrEmpty(defaultCurrency))
                        defaultCurrency = "eur";

                    bool usersAreExternallyManaged = false;

                    string externalAuthorizationMethod = tenant.ExternalAuthenticationMethod;

                    // external authorization is optional

                    string oidcClientId = null;
                    string oidcClientSecret = null;
                    string oidcEndpointUrl = null;

                    string samlEntityId = null;

                    string samlMetadataLocation = null;

                    string samlProviderUrl = null;
                    
                    X509Certificate2 samlCertificate = null;

                    bool samlAllowWeakSigningAlgorithm = false;

                    string externalDirectoryType = null;
                    string externalDirectoryHost = null;
                    int? externalDirectoryPort = null;

                    bool? externalDirectoryUsesSsl = null;

                    X509Certificate2 externalDirectorySslCertificate = null;

                    string externalDirectoryAccountDistinguishedName = null;

                    string externalDirectoryPassword = null;

                    string externalDirectoryLoginAttribute = null;

                    string externalDirectoryBaseContexts = null;

                    string externalDirectoryUserFilter = null;
                    string externalDirectoryGroupFilter = null;

                    int? externalDirectorySyncIntervalSeconds = null;

                    string externalDirectoryAdministratorGroupUuid = null;

                    if (!string.IsNullOrEmpty(externalAuthorizationMethod))
                    {
                        if (!externalAuthorizationMethod.Equals(TenantConstants.ExternalAuthenticationMethodOidc) && !externalAuthorizationMethod.Equals(TenantConstants.ExternalAuthenticationMethodSaml))
                            throw new Exception("The tenant external authentication method specification is invalid");

                        if (externalAuthorizationMethod.Equals(TenantConstants.ExternalAuthenticationMethodOidc))
                        {
                            oidcClientId = tenant.OidcClientId;
                            if (string.IsNullOrEmpty(oidcClientId))
                                throw new Exception("The tenant OIDC client ID is missing");

                            oidcClientSecret = tenant.OidcClientSecret;
                            if (string.IsNullOrEmpty(oidcClientSecret))
                                throw new Exception("The tenant OIDC client secret is missing");

                            oidcEndpointUrl = tenant.OidcEndpointUrl;
                            if (string.IsNullOrEmpty(oidcEndpointUrl))
                                throw new Exception("The tenant OIDC endpoint URL is missing");
                        }
                        else if (externalAuthorizationMethod.Equals(TenantConstants.ExternalAuthenticationMethodSaml))
                        {
                            samlEntityId = tenant.SamlEntityId;
                            if (string.IsNullOrEmpty(samlEntityId))
                                throw new Exception("The tenant SAML entity ID is missing");

                            samlMetadataLocation = tenant.SamlMetadataLocation;
                            if (string.IsNullOrEmpty(samlMetadataLocation))
                                throw new Exception("The tenant SAML metadata location is missing");

                            samlProviderUrl = tenant.SamlProviderUrl;
                            if (string.IsNullOrEmpty(samlProviderUrl))
                                throw new Exception("The tenant SAML provider URL is missing");

                            if (!string.IsNullOrEmpty(tenant.SamlCertificate))
                                samlCertificate = new X509Certificate2(GetCertificateBytesFromPEM(tenant.SamlCertificate));
                            else
                            {
                                samlCertificate = standardSamlCertificate;
                            }

                            samlAllowWeakSigningAlgorithm = tenant.SamlAllowWeakSigningAlgorithm ?? false;
                        }

                        externalDirectoryType = tenant.ExternalDirectoryType;

                        if (string.IsNullOrEmpty(externalDirectoryType))
                            throw new Exception("The tenant external directory type is missing");

                        if (/* !string.Equals(externalDirectoryType, DirectoryConstants.DirectoryTypeAD) &&
                            !string.Equals(externalDirectoryType, DirectoryConstants.DirectoryTypeADLDS) &&
                            !string.Equals(externalDirectoryType, DirectoryConstants.DirectoryTypeLDAP) && */
                            !string.Equals(externalDirectoryType, DirectoryConstants.DirectoryTypeDynamic))
                        {
                            throw new Exception("The tenant external directory type specification is invalid");
                        }

                        // TODO: LDAP etc. is not yet implemented

                        if (!string.Equals(externalDirectoryType, DirectoryConstants.DirectoryTypeDynamic))
                        {
                            // dynamic directory types will create users on-demand when logging in

                            externalDirectoryHost = tenant.ExternalDirectoryHost;

                            if (string.IsNullOrEmpty(externalDirectoryHost))
                                throw new Exception("The tenant external directory host is missing");

                            externalDirectoryUsesSsl = tenant.ExternalDirectoryUsesSsl ?? false;

                            externalDirectoryPort = tenant.ExternalDirectoryPort;

                            if (externalDirectoryPort != null && (externalDirectoryPort <= 0 || externalDirectoryPort >= ushort.MaxValue))
                                throw new Exception("The tenant external directory port is invalid");

                            if (!string.IsNullOrEmpty(tenant.ExternalDirectorySslCertificate))
                                externalDirectorySslCertificate = new X509Certificate2(GetCertificateBytesFromPEM(tenant.ExternalDirectorySslCertificate));

                            if ((bool)externalDirectoryUsesSsl && externalDirectorySslCertificate == null)
                                throw new Exception("The tenant external directory SSL certificate is missing");

                            externalDirectoryAccountDistinguishedName = tenant.ExternalDirectoryAccountDistinguishedName;
                            if (string.IsNullOrEmpty(externalDirectoryAccountDistinguishedName))
                                throw new Exception("The tenant external directory account distinguished name is missing");

                            externalDirectoryPassword = tenant.ExternalDirectoryPassword;
                            if (string.IsNullOrEmpty(externalDirectoryPassword))
                                throw new Exception("The tenant external directory password is missing");

                            externalDirectoryLoginAttribute = tenant.ExternalDirectoryLoginAttribute;

                            externalDirectoryBaseContexts = tenant.ExternalDirectoryBaseContexts;
                            if (string.IsNullOrEmpty(externalDirectoryBaseContexts))
                                throw new Exception("The tenant external directory base contexts are missing");

                            externalDirectoryUserFilter = tenant.ExternalDirectoryUserFilter;
                            externalDirectoryGroupFilter = tenant.ExternalDirectoryGroupFilter;

                            externalDirectorySyncIntervalSeconds = tenant.ExternalDirectorySyncIntervalSeconds;

                            if (externalDirectorySyncIntervalSeconds != null && externalDirectorySyncIntervalSeconds < 1)
                                throw new Exception("The tenant external directory sync interval is invalid");

                            externalDirectoryAdministratorGroupUuid = tenant.ExternalDirectoryAdministratorGroupUuid;
                            if (string.IsNullOrEmpty(externalDirectoryAdministratorGroupUuid))
                                throw new Exception("The tenant external directory administrator group UUID is missing");
                        }

                        usersAreExternallyManaged = true;
                    }

                    var tenantInfo = new TenantInfoImpl()
                    {
                        DeveloperUuid = developer.Uuid,
                        DeveloperAuthority = developer.Authority,
                        DeveloperAudience = developer.Audience,
                        DeveloperCertificate = developerCertificate,
                        DeveloperAuthCookieDomain = developer.AuthCookieDomain,
                        DeveloperName = developer.Name,
                        DeveloperPrivacyPolicyUrl = developer.PrivacyPolicyUrl,
                        DeveloperPrivacyPolicyVersion = developer.PrivacyPolicyVersion,
                        DeveloperTermsAndConditionsUrl = developer.TermsAndConditionsUrl,
                        DeveloperTermsAndConditionsVersion = developer.TermsAndConditionsVersion,
                        TenantUuid = tenant.Uuid,
                        Name = tenant.Name,
                        RequiresTermsAndConditions = requiresTermsAndConditions ?? true,
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
                        CustomInvitationEmailTextPrefix = customInvitationEmailTextPrefix,
                        CustomInvitationEmailTextSuffix = customInvitationEmailTextSuffix,
                        ProductName = productName,
                        BackendApiUrl = tenant.BackendApiUrl,
                        FrontendApiUrl = tenant.FrontendApiUrl,
                        WebUrl = tenant.WebUrl,
                        DefaultCulture = defaultCulture,
                        DefaultCurrency = defaultCurrency,
                        UsersAreExternallyManaged = usersAreExternallyManaged,
                        ExternalAuthenticationMethod = externalAuthorizationMethod,
                        OidcClientId = oidcClientId,
                        OidcClientSecret = oidcClientSecret,
                        OidcEndpointUrl = oidcEndpointUrl,     
                        SamlEntityId = samlEntityId,
                        SamlMetadataLocation = samlMetadataLocation,
                        SamlProviderUrl = samlProviderUrl,
                        SamlCertificate = samlCertificate,
                        SamlAllowWeakSigningAlgorithm = samlAllowWeakSigningAlgorithm,
                        ExternalDirectoryType = externalDirectoryType,
                        ExternalDirectoryHost = externalDirectoryHost,
                        ExternalDirectoryPort = externalDirectoryPort,
                        ExternalDirectoryUsesSsl = externalDirectoryUsesSsl,
                        ExternalDirectorySslCertificate = externalDirectorySslCertificate,
                        ExternalDirectoryAccountDistinguishedName = externalDirectoryAccountDistinguishedName,
                        ExternalDirectoryPassword = externalDirectoryPassword,
                        ExternalDirectoryLoginAttribute = externalDirectoryLoginAttribute,
                        ExternalDirectoryBaseContexts = externalDirectoryBaseContexts,
                        ExternalDirectoryUserFilter = externalDirectoryUserFilter,
                        ExternalDirectoryGroupFilter = externalDirectoryGroupFilter,
                        ExternalDirectorySyncIntervalSeconds = externalDirectorySyncIntervalSeconds,
                        ExternalDirectoryAdministratorGroupUuid = externalDirectoryAdministratorGroupUuid
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

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            int httpHealthCheckPort = configuration.GetValue<int>("WebServer:HttpHealthCheckPort");
            string tenantHealthCheckTenant = configuration["Identity:Tenants:HealthCheckTenant"];

            if (httpHealthCheckPort > 0 &&
                !string.IsNullOrEmpty(tenantHealthCheckTenant))
            {
                HealthCheckPort = httpHealthCheckPort;
                HealthCheckTenantHost = tenantHealthCheckTenant;
            }

            var standardSamlCertificate = ReadStandardSamlCertificate(configuration);

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
                        var developerWrapper = new DeveloperWrapper(developer, standardSamlCertificate);

                        _developerMappings.Add(developer.HostPattern, developerWrapper);
                        _developerMappingsByUuid.Add(developer.Uuid, developerWrapper);

                        Tenants.AddRange(developerWrapper.TenantInfos);

                        X509Certificate2 developerCertificate = null;

                        if (!string.IsNullOrEmpty(developer.Certificate))
                        {
                            if (string.IsNullOrEmpty(developer.CertificatePassword))
                                throw new Exception("Developer in tenant database has certificate set, but no certificate password is present");

                            developerCertificate = new X509Certificate2(GetCertificateBytesFromPEM(developer.Certificate), developer.CertificatePassword);
                        }

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
                            Certificate = developerCertificate,
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

        public (string, ITenantInfo) LookupTenantByHost(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                _logger.LogDebug($"Host is empty for tenant parsing");

                return (null, null);
            }
            
            string[] parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                // prefix with www

                host = "www." + host;
            }

            parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
            {
                _logger.LogDebug($"Host {host} does not have enough parts for tenant parsing");

                return (null, null);
            }

            if (parts.Length > 4)
            {
                _logger.LogDebug($"Host {host} has too many parts for tenant parsing");

                return (null, null);
            }

            // first part

            string subDomainLookup = parts[0];

            // two last parts, without (optional) .clapi, .auth, ... subdomains

            string hostLookup = parts[parts.Length - 2] + "." + parts[parts.Length - 1];

            if (!_developerMappings.ContainsKey(hostLookup))
            {
                _logger.LogError($"No developer found for host {host}");

                return (null, null);
            }

            var developerWrapper = _developerMappings[hostLookup];
            if (developerWrapper == null)
            {
                _logger.LogError($"No developer found for host {host}");

                return (null, null);
            }

            var tenantInfo = developerWrapper.LookupTenantBySubDomain(subDomainLookup);
            if (tenantInfo == null)
            {
                _logger.LogError($"No developer found for host {host}, developer {developerWrapper.Developer.Uuid} and sub domain lookup {subDomainLookup}");

                return (null, null);
            }

            return (subDomainLookup, tenantInfo);
        }

        public ITenantInfo LookupTenantByUuid(long developerUuid, long tenantUuid)
        {
            if (!_developerMappingsByUuid.ContainsKey(developerUuid))
                return null;

            return _developerMappingsByUuid[developerUuid].LookupTenantByUuid(tenantUuid);
        }

        internal static byte[] GetCertificateBytesFromPEM(string pemString)
        {
            const string section = "CERTIFICATE";

            var header = String.Format("-----BEGIN {0}-----", section);
            var footer = String.Format("-----END {0}-----", section);

            var start = pemString.IndexOf(header, StringComparison.Ordinal);
            if (start < 0)
                return null;

            start += header.Length;
            var end = pemString.IndexOf(footer, start, StringComparison.Ordinal) - start;

            if (end < 0)
                return null;

            return Convert.FromBase64String(pemString.Substring(start, end));
        }

        private X509Certificate2 ReadStandardSamlCertificate(IConfiguration configuration)
        {
            string samlCertificateAssembly = configuration["Identity:Saml:Certificate:Assembly"];
            if (string.IsNullOrEmpty(samlCertificateAssembly))
                throw new Exception("SAML certificate assembly not found");

            string samlCertificateName = configuration["Identity:Saml:Certificate:Name"];

            if (string.IsNullOrEmpty(samlCertificateName))
                throw new Exception("SAML certificate name not found");

            string samlCertificatePassword = configuration["Identity:Saml:Certificate:Password"];

            if (string.IsNullOrEmpty(samlCertificatePassword))
                throw new Exception("SAML certificate password not found");

            X509Certificate2 certificate = null;

            Assembly samlAssembly = AppDomain.CurrentDomain.GetAssemblies().
                SingleOrDefault(assembly => assembly.GetName().Name == samlCertificateAssembly);

            if (samlAssembly == null)
                throw new Exception("SAML certificate assembly is not present in the list of assemblies");

            var resourceStream = samlAssembly.GetManifestResourceStream(samlCertificateName);

            if (resourceStream == null)
                throw new Exception("SAML certificate resource not found");

            using (var memory = new MemoryStream((int)resourceStream.Length))
            {
                resourceStream.CopyTo(memory);

                certificate = new X509Certificate2(memory.ToArray(), samlCertificatePassword);
            }

            return certificate;
        }
    }
}
