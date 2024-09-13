using HCore.Directory;
using HCore.Storage;
using HCore.Tenants.Cache;
using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Database.SqlServer.Models.Impl;
using HCore.Tenants.Models;
using HCore.Tenants.Models.Impl;
using HCore.Web.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace HCore.Tenants.Providers.Impl
{
    internal class TenantDataProviderImpl : ITenantDataProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ITenantCache _tenantCache;

        private readonly Dictionary<string, IDeveloperInfo> _developerInfosByHostPattern;

        private readonly Dictionary<long, IDeveloperInfo> _developerInfosByUuid;

        public List<IDeveloperInfo> Developers { get; internal set; }
        public List<string> DeveloperWildcardSubdomains { get; internal set; }

        public int? HealthCheckPort { get; internal set; }
        public string HealthCheckTenantHost { get; internal set; }

        private byte[] _standardSamlCertificateBytes;
        private string _standardSamlCertificatePassword;

        private readonly ILogger<TenantDataProviderImpl> _logger;

        public TenantDataProviderImpl(IServiceProvider serviceProvider, ITenantCache tenantCache, ILogger<TenantDataProviderImpl> logger)
        {
            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            _tenantCache = tenantCache;

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            int httpHealthCheckPort = configuration.GetValue<int>("WebServer:HttpHealthCheckPort");
            string tenantHealthCheckTenant = configuration["Identity:Tenants:HealthCheckTenant"];

            if (httpHealthCheckPort > 0 &&
                !string.IsNullOrEmpty(tenantHealthCheckTenant))
            {
                HealthCheckPort = httpHealthCheckPort;
                HealthCheckTenantHost = tenantHealthCheckTenant;
            }

            ReadStandardSamlCertificate(configuration);

            using (var scope = _scopeFactory.CreateScope())
            {
                using (var dbContext = scope.ServiceProvider.GetRequiredService<SqlServerTenantDbContext>())
                {
                    IQueryable<DeveloperModel> query = dbContext.Developers;

                    List<DeveloperModel> developerModels = query.ToList();

                    _developerInfosByHostPattern = new Dictionary<string, IDeveloperInfo>();
                    _developerInfosByUuid = new Dictionary<long, IDeveloperInfo>();

                    Developers = new List<IDeveloperInfo>();
                    DeveloperWildcardSubdomains = new List<string>();

                    developerModels.ForEach(developerModel =>
                    {
                        X509Certificate2 developerCertificate = null;

                        if (!string.IsNullOrEmpty(developerModel.Certificate))
                        {
                            if (string.IsNullOrEmpty(developerModel.CertificatePassword))
                                throw new Exception("Developer in tenant database has certificate set, but no certificate password is present");

                            developerCertificate = new X509Certificate2(GetCertificateBytesFromPEM(developerModel.Certificate), developerModel.CertificatePassword);
                        }

                        if (string.IsNullOrEmpty(developerModel.Authority))
                            throw new Exception("The developer authority is empty");

                        if (string.IsNullOrEmpty(developerModel.Audience))
                            throw new Exception("The developer audience is empty");

                        if (string.IsNullOrEmpty(developerModel.AuthCookieDomain))
                            throw new Exception("The developer auth cookie domain is empty");

                        if (string.IsNullOrEmpty(developerModel.HostPattern))
                            throw new Exception("The developer host pattern is empty");

                        if (string.IsNullOrEmpty(developerModel.DefaultEcbBackendApiUrlSuffix))
                            developerModel.DefaultEcbBackendApiUrlSuffix = null;

                        if (string.IsNullOrEmpty(developerModel.DefaultPortalsBackendApiUrlSuffix))
                            throw new Exception("The developer default Portals backend API URL suffix is empty");

                        if (string.IsNullOrEmpty(developerModel.DefaultFrontendApiUrlSuffix))
                            throw new Exception("The developer default frontend API URL suffix is empty");

                        if (string.IsNullOrEmpty(developerModel.DefaultWebUrlSuffix))
                            throw new Exception("The developer default web URL suffix is empty");

                        if (string.IsNullOrEmpty(developerModel.Name))
                            throw new Exception("The developer name is empty");

                        if (string.IsNullOrEmpty(developerModel.LogoSvgUrl))
                            throw new Exception("The developer logo SVG URL is empty");

                        if (string.IsNullOrEmpty(developerModel.LogoPngUrl))
                            throw new Exception("The developer logo PNG URL is empty");

                        if (string.IsNullOrEmpty(developerModel.IconIcoUrl))
                            throw new Exception("The developer icon ICO URL is empty");

                        // storage is optional
                        
                        string storageImplementation = developerModel.StorageImplementation;
                        if (!string.IsNullOrEmpty(storageImplementation))
                        {
                            if (!storageImplementation.Equals(StorageConstants.StorageImplementationAzure) && !storageImplementation.Equals(StorageConstants.StorageImplementationGoogleCloud))
                                throw new Exception("The developer storage implementation specification is invalid");

                            if (string.IsNullOrEmpty(developerModel.StorageConnectionString))
                                throw new Exception("The developer storage connection string is empty");
                        }

                        if (string.IsNullOrEmpty(developerModel.SupportEmail))
                            throw new Exception("The developer support email is empty");

                        if (string.IsNullOrEmpty(developerModel.SupportEmailDisplayName))
                            throw new Exception("The developer support email display name is empty");

                        if (string.IsNullOrEmpty(developerModel.NoreplyEmail))
                            throw new Exception("The developer noreply email is empty");

                        if (string.IsNullOrEmpty(developerModel.NoreplyEmailDisplayName))
                            throw new Exception("The developer noreply email display name is empty");

                        var ecbProductName = developerModel.EcbProductName;
                        if (string.IsNullOrEmpty(ecbProductName))
                            throw new Exception("The developer ECB product name is empty");

                        var portalsProductName = developerModel.PortalsProductName;
                        if (string.IsNullOrEmpty(portalsProductName))
                            throw new Exception("The developer Portals product name is empty");

                        var emailSettingsModel = developerModel.GetEmailSettings();
                        if (emailSettingsModel == null)
                            throw new Exception("The developer email settings are missing");

                        emailSettingsModel.Validate(allowEmpty: false);

                        var developerInfo = new DeveloperInfoImpl()
                        {
                            DeveloperUuid = developerModel.Uuid,
                            Authority = developerModel.Authority,
                            Audience = developerModel.Audience,
                            Certificate = developerCertificate,
                            AuthCookieDomain = developerModel.AuthCookieDomain,
                            HostPattern = developerModel.HostPattern,
                            DefaultEcbBackendApiUrlSuffix = developerModel.DefaultEcbBackendApiUrlSuffix,
                            DefaultPortalsBackendApiUrlSuffix = developerModel.DefaultPortalsBackendApiUrlSuffix,
                            DefaultFrontendApiUrlSuffix = developerModel.DefaultFrontendApiUrlSuffix,
                            DefaultWebUrlSuffix = developerModel.DefaultWebUrlSuffix,
                            Name = developerModel.Name,
                            LogoSvgUrl = developerModel.LogoSvgUrl,
                            LogoPngUrl = developerModel.LogoPngUrl,
                            IconIcoUrl = developerModel.IconIcoUrl,
                            AppleTouchIconUrl = developerModel.AppleTouchIconUrl,
                            CustomCss = developerModel.CustomCss,
                            CustomEmailCss = developerModel.CustomEmailCss,
                            StorageImplementation = developerModel.StorageImplementation,
                            StorageConnectionString = developerModel.StorageConnectionString,
                            PrimaryColor = developerModel.PrimaryColor,
                            SecondaryColor = developerModel.SecondaryColor,
                            TextOnPrimaryColor = developerModel.TextOnPrimaryColor,
                            TextOnSecondaryColor = developerModel.TextOnSecondaryColor,
                            SupportEmail = developerModel.SupportEmail,
                            SupportEmailDisplayName = developerModel.SupportEmailDisplayName,
                            NoreplyEmail = developerModel.NoreplyEmail,
                            NoreplyEmailDisplayName = developerModel.NoreplyEmailDisplayName,
                            WebAddress = developerModel.WebAddress,
                            PoweredByShort = developerModel.PoweredByShort,
                            HidePoweredBy = developerModel.HidePoweredBy,
                            EmailSettings = emailSettingsModel,
                            EcbProductName = ecbProductName,
                            PortalsProductName = portalsProductName
                        };

                        _developerInfosByHostPattern.Add(developerModel.HostPattern, developerInfo);
                        _developerInfosByUuid.Add(developerModel.Uuid, developerInfo);

                        Developers.Add(developerInfo);

                        DeveloperWildcardSubdomains.Add($"https://*.{developerModel.HostPattern}");
                        DeveloperWildcardSubdomains.Add($"https://*.{developerModel.HostPattern}:*");
                    });
                }
            }

            _logger = logger;
        }

        public IDeveloperInfo GetDeveloper(long developerUuid)
        {
            if (!_developerInfosByUuid.ContainsKey(developerUuid))
                return null;

            return _developerInfosByUuid[developerUuid];
        }

        public async Task<(string, ITenantInfo)> GetTenantByHostAsync(string host, HttpContext context = null)
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

            var hostLookup = parts[parts.Length - 2] + "." + parts[parts.Length - 1];

            if (!_developerInfosByHostPattern.ContainsKey(hostLookup))
            {
                _logger.LogInformation($"No developer info found for host {host}");

                return (null, null);
            }

            var developerInfo = _developerInfosByHostPattern[hostLookup];
            if (developerInfo == null)
            {
                _logger.LogInformation($"No developer info found for host {host}");

                return (null, null);
            }

            var tenantInfo = await _tenantCache.GetTenantInfoBySubdomainLookupAsync(developerInfo.DeveloperUuid, subDomainLookup, cultureInfo: null, isDraft: false).ConfigureAwait(false);

            if (tenantInfo == null)
            {
                tenantInfo = await GetTenantInfoBySubdomainLookupAsync(developerInfo, subDomainLookup).ConfigureAwait(false);

                if (tenantInfo == null)
                {
                    _logger.LogInformation($"No developer found for host {host}, developer {developerInfo.DeveloperUuid} and sub domain lookup {subDomainLookup}");

                    return (null, null);
                }

                await _tenantCache.CreateOrUpdateTenantInfoForSubdomainLookupAsync(developerInfo.DeveloperUuid, subDomainLookup, cultureInfo: null, isDraft: false, tenantInfo).ConfigureAwait(false);
            }

            return (subDomainLookup, tenantInfo);
        }

        private async Task<ITenantInfo> GetTenantInfoBySubdomainLookupAsync(IDeveloperInfo developerInfo, string subDomainLookup)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                using (var dbContext = scope.ServiceProvider.GetRequiredService<SqlServerTenantDbContext>())
                {
                    IQueryable<TenantModel> query = dbContext.Tenants;

                    query = query.Where(tenant => tenant.DeveloperUuid == developerInfo.DeveloperUuid);
                    query = query.Where(tenant => tenant.SubdomainPatterns.Contains(subDomainLookup));

                    query = query.Include(tenant => tenant.Developer);

                    query = query.Include(tenant => tenant.Subscriptions);

                    var tenantModel = await query.FirstOrDefaultAsync().ConfigureAwait(false);

                    if (tenantModel == null)
                        return null;

                    return ConvertToTenantInfo(tenantModel);
                }
            }
        }

        public async Task<ITenantInfo> GetTenantByUuidThrowAsync(long developerUuid, long tenantUuid)
        {
            if (!_developerInfosByUuid.ContainsKey(developerUuid))
                throw new NotFoundApiException(NotFoundApiException.TenantNotFound, $"No tenant found for developer UUID {developerUuid} and tenant UUID {tenantUuid}");

            var developerInfo = _developerInfosByUuid[developerUuid];

            var tenantInfo = await _tenantCache.GetTenantInfoByUuidAsync(developerUuid, tenantUuid, cultureInfo: null, isDraft: false).ConfigureAwait(false);

            if (tenantInfo == null)
            {
                tenantInfo = await GetTenantInfoByUuidAsync(developerInfo, tenantUuid).ConfigureAwait(false);

                if (tenantInfo == null)
                {
                    throw new NotFoundApiException(NotFoundApiException.TenantNotFound, $"No tenant found for developer UUID {developerUuid} and tenant UUID {tenantUuid}");
                }

                await _tenantCache.CreateOrUpdateTenantInfoForUuidAsync(developerUuid, tenantUuid, cultureInfo: null, isDraft: false, tenantInfo).ConfigureAwait(false);
            }

            return tenantInfo;
        }

        private async Task<ITenantInfo> GetTenantInfoByUuidAsync(IDeveloperInfo developerInfo, long tenantUuid)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                using (var dbContext = scope.ServiceProvider.GetRequiredService<SqlServerTenantDbContext>())
                {
                    IQueryable<TenantModel> query = dbContext.Tenants;

                    query = query.Where(tenant => tenant.DeveloperUuid == developerInfo.DeveloperUuid);
                    query = query.Where(tenant => tenant.Uuid == tenantUuid);

                    query = query.Include(tenant => tenant.Developer);

                    query = query.Include(tenant => tenant.Subscriptions);

                    var tenantModel = await query.FirstOrDefaultAsync().ConfigureAwait(false);

                    if (tenantModel == null)
                        return null;

                    return ConvertToTenantInfo(tenantModel);
                }
            }
        }

        private ITenantInfo ConvertToTenantInfo(TenantModel tenantModel)
        {
            var developerModel = tenantModel.Developer;

            byte[] developerCertificateBytes = null;
            string developerCertificatePassword = null;

            if (!string.IsNullOrEmpty(developerModel.Certificate))
            {
                if (string.IsNullOrEmpty(developerModel.CertificatePassword))
                    throw new Exception("Developer in tenant database has certificate set, but no certificate password is present");

                developerCertificateBytes = GetCertificateBytesFromPEM(developerModel.Certificate);
                developerCertificatePassword = developerModel.CertificatePassword;
            }

            if (tenantModel.SubdomainPatterns == null || tenantModel.SubdomainPatterns.Length == 0)
                throw new Exception("The tenant subdomain patterns are empty");

            if (string.IsNullOrEmpty(tenantModel.Name))
                throw new Exception("The tenant name is empty");

            if (string.IsNullOrEmpty(tenantModel.FrontendApiUrl))
                throw new Exception("The tenant frontend API URL is empty");

            if (string.IsNullOrEmpty(tenantModel.EcbBackendApiUrl))
                tenantModel.EcbBackendApiUrl = null;

            if (string.IsNullOrEmpty(tenantModel.PortalsBackendApiUrl))
                throw new Exception("The tenant Portals backend API url is empty");

            if (string.IsNullOrEmpty(tenantModel.WebUrl))
                throw new Exception("The tenant web url is empty");

            bool? requiresTermsAndConditions = tenantModel.RequiresTermsAndConditions;
            if (requiresTermsAndConditions == null)
                requiresTermsAndConditions = developerModel.RequiresTermsAndConditions;

            string logoSvgUrl = tenantModel.LogoSvgUrl;
            if (string.IsNullOrEmpty(logoSvgUrl))
                logoSvgUrl = developerModel.LogoSvgUrl;

            string logoPngUrl = tenantModel.LogoPngUrl;
            if (string.IsNullOrEmpty(logoPngUrl))
                logoPngUrl = developerModel.LogoPngUrl;

            string iconIcoUrl = tenantModel.IconIcoUrl;
            if (string.IsNullOrEmpty(iconIcoUrl))
                iconIcoUrl = developerModel.IconIcoUrl;

            string appleTouchIconUrl = tenantModel.AppleTouchIconUrl;
            if (string.IsNullOrEmpty(appleTouchIconUrl))
                appleTouchIconUrl = developerModel.AppleTouchIconUrl;

            string customCss = tenantModel.CustomCss;
            if (string.IsNullOrEmpty(customCss))
                customCss = developerModel.CustomCss;

            string storageImplementation = tenantModel.StorageImplementation;
            if (string.IsNullOrEmpty(storageImplementation))
                storageImplementation = developerModel.StorageImplementation;

            // storage is optional

            string storageConnectionString = null;

            if (!string.IsNullOrEmpty(storageImplementation))
            {
                if (!storageImplementation.Equals(StorageConstants.StorageImplementationAzure) && !storageImplementation.Equals(StorageConstants.StorageImplementationGoogleCloud))
                    throw new Exception("The tenant storage implementation specification is invalid");

                storageConnectionString = tenantModel.StorageConnectionString;
                if (string.IsNullOrEmpty(storageConnectionString))
                    storageConnectionString = developerModel.StorageConnectionString;
            }

            int? primaryColor = tenantModel.PrimaryColor;
            if (primaryColor == null)
                primaryColor = developerModel.PrimaryColor;

            int? secondaryColor = tenantModel.SecondaryColor;
            if (secondaryColor == null)
                secondaryColor = developerModel.SecondaryColor;

            int? textOnPrimaryColor = tenantModel.TextOnPrimaryColor;
            if (textOnPrimaryColor == null)
                textOnPrimaryColor = developerModel.TextOnPrimaryColor;

            int? textOnSecondaryColor = tenantModel.TextOnSecondaryColor;
            if (textOnSecondaryColor == null)
                textOnSecondaryColor = developerModel.TextOnSecondaryColor;

            string supportEmail = tenantModel.SupportEmail;
            if (string.IsNullOrEmpty(supportEmail))
                supportEmail = developerModel.SupportEmail;

            string supportEmailDisplayName = tenantModel.SupportEmailDisplayName;
            if (string.IsNullOrEmpty(supportEmailDisplayName))
                supportEmailDisplayName = developerModel.SupportEmailDisplayName;

            var permissionDeniedSupportMessage = tenantModel.PermissionDeniedSupportMessage;

            string noreplyEmail = tenantModel.NoreplyEmail;
            if (string.IsNullOrEmpty(noreplyEmail))
                noreplyEmail = developerModel.NoreplyEmail;

            string noreplyEmailDisplayName = tenantModel.NoreplyEmailDisplayName;
            if (string.IsNullOrEmpty(noreplyEmailDisplayName))
                noreplyEmailDisplayName = developerModel.NoreplyEmailDisplayName;

            var emailSettingsModel = developerModel.GetEmailSettings();
            var customEmailSettingsModel = tenantModel.GetCustomEmailSettings();

            if (emailSettingsModel != null &&
                customEmailSettingsModel != null)
            {
                var targetEmailSettingsModel = new EmailSettingsModel();

                targetEmailSettingsModel.MergeWith(emailSettingsModel, allowEmpty: false);
                targetEmailSettingsModel.MergeWith(customEmailSettingsModel, allowEmpty: false);

                emailSettingsModel = targetEmailSettingsModel;

                emailSettingsModel.Validate(allowEmpty: false);
            }

            var ecbProductName = tenantModel.EcbProductName;
            if (string.IsNullOrEmpty(ecbProductName))
                ecbProductName = developerModel.EcbProductName;

            var portalsProductName = tenantModel.PortalsProductName;
            if (string.IsNullOrEmpty(portalsProductName))
                portalsProductName = developerModel.PortalsProductName;

            string defaultCulture = tenantModel.DefaultCulture;
            if (string.IsNullOrEmpty(defaultCulture))
                defaultCulture = "en";

            string defaultCurrency = tenantModel.DefaultCurrency;
            if (string.IsNullOrEmpty(defaultCurrency))
                defaultCurrency = "eur";

            byte[] httpsCertificateBytes = null;
            string httpsCertificatePassword = null;

            if (!string.IsNullOrEmpty(tenantModel.HttpsCertificate))
            {
                httpsCertificateBytes = Convert.FromBase64String(tenantModel.HttpsCertificate);
                httpsCertificatePassword = tenantModel.HttpsCertificatePassword;
            }

            bool usersAreExternallyManaged = false;
            bool externalUsersAreManuallyManaged = false;

            string externalAuthorizationMethod = tenantModel.ExternalAuthenticationMethod;

            bool externalAuthenticationAllowLocalLogin = tenantModel.ExternalAuthenticationAllowLocalLogin;
            bool externalAuthenticationAllowUserMerge = tenantModel.ExternalAuthenticationAllowUserMerge;

            Dictionary<string, string> externalAuthenticationClaimMappings = null;

            // external authorization is optional

            string oidcClientId = null;
            string oidcClientSecret = null;
            string oidcEndpointUrl = null;
            bool oidcUsePkce = false;
            string[] oidcScopes = null;
            string oidcAcrValues = null;
            string oidcAcrValuesAppendix = null;
            bool oidcTriggerAcrValuesAppendixByUrlParameter = false;
            bool oidcQueryUserInfoEndpoint = true;
            Dictionary<string, string> oidcAdditionalParameters = null;

            string samlEntityId = null;

            string samlPeerEntityId = null;
            string samlPeerIdpMetadataLocation = null;
            string samlPeerIdpMetadata = null;

            byte[] samlCertificateBytes = null;
            string samlCertificatePassword = null;

            bool samlAllowWeakSigningAlgorithm = false;

            string externalDirectoryType = null;
            string externalDirectoryHost = null;
            int? externalDirectoryPort = null;

            bool? externalDirectoryUsesSsl = null;

            byte[] externalDirectorySslCertificateBytes = null;
            string externalDirectorySslCertificatePassword = null;

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
                    oidcClientId = tenantModel.OidcClientId;
                    if (string.IsNullOrEmpty(oidcClientId))
                        throw new Exception("The tenant OIDC client ID is missing");

                    oidcUsePkce = tenantModel.OidcUsePkce;

                    if (!oidcUsePkce)
                    {
                        oidcClientSecret = tenantModel.OidcClientSecret;
                        if (string.IsNullOrEmpty(oidcClientSecret))
                            throw new Exception("The tenant OIDC client secret is missing");
                    }
                    else
                    {
                        oidcClientSecret = null;
                    }

                    oidcEndpointUrl = tenantModel.OidcEndpointUrl;
                    if (string.IsNullOrEmpty(oidcEndpointUrl))
                        throw new Exception("The tenant OIDC endpoint URL is missing");
                    
                    oidcScopes = tenantModel.OidcScopes;

                    oidcAcrValues = tenantModel.OidcAcrValues;

                    oidcAcrValuesAppendix = tenantModel.OidcAcrValuesAppendix;
                    oidcTriggerAcrValuesAppendixByUrlParameter = tenantModel.OidcTriggerAcrValuesAppendixByUrlParameter;

                    oidcQueryUserInfoEndpoint = tenantModel.OidcQueryUserInfoEndpoint;

                    oidcAdditionalParameters = tenantModel.OidcAdditionalParameters;
                }
                else if (externalAuthorizationMethod.Equals(TenantConstants.ExternalAuthenticationMethodSaml))
                {
                    samlEntityId = tenantModel.SamlEntityId;
                    if (string.IsNullOrEmpty(samlEntityId))
                        throw new Exception("The tenant SAML entity ID is missing");

                    samlPeerEntityId = tenantModel.SamlPeerEntityId;
                    if (string.IsNullOrEmpty(samlPeerEntityId))
                        throw new Exception("The tenant SAML peer entity ID is missing");

                    samlPeerIdpMetadataLocation = tenantModel.SamlPeerIdpMetadataLocation;
                    samlPeerIdpMetadata = tenantModel.SamlPeerIdpMetadata;

                    if (!string.IsNullOrEmpty(samlPeerIdpMetadataLocation) && !string.IsNullOrEmpty(samlPeerIdpMetadata))
                        throw new Exception("Please give either the SAML peer IDP metadata location or the metadata itself, not both");

                    if (string.IsNullOrEmpty(samlPeerIdpMetadataLocation) && string.IsNullOrEmpty(samlPeerIdpMetadata))
                        throw new Exception("The tenant SAML peer IDP metadata location or the metadata itself is missing");

                    if (!string.IsNullOrEmpty(tenantModel.SamlCertificate))
                    {
                        samlCertificateBytes = GetCertificateBytesFromPEM(tenantModel.SamlCertificate);
                        samlCertificatePassword = tenantModel.SamlCertificatePassword;
                    }
                    else
                    {
                        samlCertificateBytes = _standardSamlCertificateBytes;
                        samlCertificatePassword = _standardSamlCertificatePassword;
                    }

                    samlAllowWeakSigningAlgorithm = tenantModel.SamlAllowWeakSigningAlgorithm ?? false;
                }

                externalAuthenticationClaimMappings = tenantModel.ExternalAuthenticationClaimMappings;

                externalDirectoryType = tenantModel.ExternalDirectoryType;

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

                    externalDirectoryHost = tenantModel.ExternalDirectoryHost;

                    if (string.IsNullOrEmpty(externalDirectoryHost))
                        throw new Exception("The tenant external directory host is missing");

                    externalDirectoryUsesSsl = tenantModel.ExternalDirectoryUsesSsl ?? false;

                    externalDirectoryPort = tenantModel.ExternalDirectoryPort;

                    if (externalDirectoryPort != null && (externalDirectoryPort <= 0 || externalDirectoryPort >= ushort.MaxValue))
                        throw new Exception("The tenant external directory port is invalid");

                    if (!string.IsNullOrEmpty(tenantModel.ExternalDirectorySslCertificate))
                    {
                        externalDirectorySslCertificateBytes = GetCertificateBytesFromPEM(tenantModel.ExternalDirectorySslCertificate);
                        externalDirectorySslCertificatePassword = null;
                    }

                    if ((bool)externalDirectoryUsesSsl && externalDirectorySslCertificateBytes == null)
                        throw new Exception("The tenant external directory SSL certificate is missing");

                    externalDirectoryAccountDistinguishedName = tenantModel.ExternalDirectoryAccountDistinguishedName;
                    if (string.IsNullOrEmpty(externalDirectoryAccountDistinguishedName))
                        throw new Exception("The tenant external directory account distinguished name is missing");

                    externalDirectoryPassword = tenantModel.ExternalDirectoryPassword;
                    if (string.IsNullOrEmpty(externalDirectoryPassword))
                        throw new Exception("The tenant external directory password is missing");

                    externalDirectoryLoginAttribute = tenantModel.ExternalDirectoryLoginAttribute;

                    externalDirectoryBaseContexts = tenantModel.ExternalDirectoryBaseContexts;
                    if (string.IsNullOrEmpty(externalDirectoryBaseContexts))
                        throw new Exception("The tenant external directory base contexts are missing");

                    externalDirectoryUserFilter = tenantModel.ExternalDirectoryUserFilter;
                    externalDirectoryGroupFilter = tenantModel.ExternalDirectoryGroupFilter;

                    externalDirectorySyncIntervalSeconds = tenantModel.ExternalDirectorySyncIntervalSeconds;

                    if (externalDirectorySyncIntervalSeconds != null && externalDirectorySyncIntervalSeconds < 1)
                        throw new Exception("The tenant external directory sync interval is invalid");

                    externalDirectoryAdministratorGroupUuid = tenantModel.ExternalDirectoryAdministratorGroupUuid;
                    if (string.IsNullOrEmpty(externalDirectoryAdministratorGroupUuid))
                        throw new Exception("The tenant external directory administrator group UUID is missing");
                }

                usersAreExternallyManaged = true;

                externalUsersAreManuallyManaged = tenantModel.ExternalUsersAreManuallyManaged;
            }

            string customTenantSettingsJson = tenantModel.CustomTenantSettingsJson;

            var tenantInfo = new TenantInfoImpl()
            {
                DeveloperUuid = developerModel.Uuid,
                DeveloperAuthority = developerModel.Authority,
                DeveloperAudience = developerModel.Audience,
                DeveloperAuthCookieDomain = developerModel.AuthCookieDomain,
                DeveloperHostPattern = developerModel.HostPattern,
                DeveloperName = developerModel.Name,
                DeveloperPrivacyPolicyUrl = developerModel.PrivacyPolicyUrl,
                DeveloperPrivacyPolicyVersion = developerModel.PrivacyPolicyVersion,
                DeveloperTermsAndConditionsUrl = developerModel.TermsAndConditionsUrl,
                DeveloperTermsAndConditionsVersion = developerModel.TermsAndConditionsVersion,
                TenantUuid = tenantModel.Uuid,
                Name = tenantModel.Name,
                SubdomainPattern = tenantModel.SubdomainPatterns[0],
                RequiresTermsAndConditions = requiresTermsAndConditions ?? true,
                LogoSvgUrl = logoSvgUrl,
                LogoPngUrl = logoPngUrl,
                IconIcoUrl = iconIcoUrl,
                AppleTouchIconUrl = appleTouchIconUrl,
                CustomCss = customCss,
                CustomEmailCss = developerModel.CustomEmailCss,
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
                SupportEmailDisplayName = supportEmailDisplayName,
                PermissionDeniedSupportMessage = permissionDeniedSupportMessage,
                NoreplyEmail = noreplyEmail,
                NoreplyEmailDisplayName = noreplyEmailDisplayName,
                WebAddress = developerModel.WebAddress,
                PoweredByShort = developerModel.PoweredByShort,
                HidePoweredBy = developerModel.HidePoweredBy,
                EmailSettings = emailSettingsModel,
                EcbProductName = ecbProductName,
                PortalsProductName = portalsProductName,
                EcbBackendApiUrl = tenantModel.EcbBackendApiUrl,
                PortalsBackendApiUrl = tenantModel.PortalsBackendApiUrl,
                FrontendApiUrl = tenantModel.FrontendApiUrl,
                WebUrl = tenantModel.WebUrl,
                DefaultCulture = defaultCulture,
                DefaultCurrency = defaultCurrency,
                Subscriptions = tenantModel.Subscriptions,
                UsersAreExternallyManaged = usersAreExternallyManaged,
                ExternalUsersAreManuallyManaged = externalUsersAreManuallyManaged,
                ExternalAuthenticationMethod = externalAuthorizationMethod,
                ExternalAuthenticationAllowLocalLogin = externalAuthenticationAllowLocalLogin,
                ExternalAuthenticationAllowUserMerge = externalAuthenticationAllowUserMerge,
                ExternalAuthenticationClaimMappings = externalAuthenticationClaimMappings,
                OidcClientId = oidcClientId,
                OidcClientSecret = oidcClientSecret,
                OidcEndpointUrl = oidcEndpointUrl,
                OidcUsePkce = oidcUsePkce,
                OidcScopes = oidcScopes,
                OidcAcrValues = oidcAcrValues,
                OidcAcrValuesAppendix = oidcAcrValuesAppendix,
                OidcTriggerAcrValuesAppendixByUrlParameter = oidcTriggerAcrValuesAppendixByUrlParameter,
                OidcQueryUserInfoEndpoint = oidcQueryUserInfoEndpoint,
                OidcAdditionalParameters = oidcAdditionalParameters,
                SamlEntityId = samlEntityId,
                SamlPeerEntityId = samlPeerEntityId,
                SamlPeerIdpMetadataLocation = samlPeerIdpMetadataLocation,
                SamlPeerIdpMetadata = samlPeerIdpMetadata,
                SamlAllowWeakSigningAlgorithm = samlAllowWeakSigningAlgorithm,
                ExternalDirectoryType = externalDirectoryType,
                ExternalDirectoryHost = externalDirectoryHost,
                ExternalDirectoryPort = externalDirectoryPort,
                ExternalDirectoryUsesSsl = externalDirectoryUsesSsl,
                ExternalDirectoryAccountDistinguishedName = externalDirectoryAccountDistinguishedName,
                ExternalDirectoryPassword = externalDirectoryPassword,
                ExternalDirectoryLoginAttribute = externalDirectoryLoginAttribute,
                ExternalDirectoryBaseContexts = externalDirectoryBaseContexts,
                ExternalDirectoryUserFilter = externalDirectoryUserFilter,
                ExternalDirectoryGroupFilter = externalDirectoryGroupFilter,
                ExternalDirectorySyncIntervalSeconds = externalDirectorySyncIntervalSeconds,
                ExternalDirectoryAdministratorGroupUuid = externalDirectoryAdministratorGroupUuid,
                CustomTenantSettingsJson = customTenantSettingsJson,
                RequiresDevAdminSsoReplacement = tenantModel.RequiresDevAdminSsoReplacement,
                DevAdminSsoReplacementSamlPeerEntityId = tenantModel.DevAdminSsoReplacementSamlPeerEntityId,
                DevAdminSsoReplacementSamlPeerIdpMetadataLocation = tenantModel.DevAdminSsoReplacementSamlPeerIdpMetadataLocation,
                EnableAudit = tenantModel.EnableAudit,
                CreatedByUserUuid = tenantModel.CreatedByUserUuid,
                MapDeveloperUuid = tenantModel.MapDeveloperUuid,
                MapTenantUuid = tenantModel.MapTenantUuid,
                MapCustomUuid = tenantModel.MapCustomUuid,
                Version = tenantModel.Version,
                CreatedAt = tenantModel.CreatedAt,
                LastUpdatedAt = tenantModel.LastUpdatedAt
            };

            tenantInfo.SetDeveloperCertificate(developerCertificateBytes, developerCertificatePassword);
            tenantInfo.SetHttpsCertificate(httpsCertificateBytes, httpsCertificatePassword);
            tenantInfo.SetSamlCertificate(samlCertificateBytes, samlCertificatePassword);
            tenantInfo.SetExternalDirectorySslCertificate(externalDirectorySslCertificateBytes, externalDirectorySslCertificatePassword);

            return tenantInfo;
        }

        private string ConvertToHexColor(int? color)
        {
            return "#" + (color != null ? ((int)color).ToString("X6") : "000000");
        }

        private byte[] GetCertificateBytesFromPEM(string pemString)
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

                _standardSamlCertificateBytes = memory.ToArray();
                _standardSamlCertificatePassword = samlCertificatePassword;
            }

            return certificate;
        }
    }
}
