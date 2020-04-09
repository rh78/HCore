using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    public class TenantModel
    {
        public const int MaxSubdomainPatternLength = 255;
        public const int MaxUrlLength = 255;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Uuid { get; set; }

        public DeveloperModel Developer { get; set; }

        [StringLength(MaxSubdomainPatternLength)]
        public string SubdomainPattern { get; set; }

        [StringLength(MaxUrlLength)]
        public string BackendApiUrl { get; set; }

        [StringLength(MaxUrlLength)]
        public string FrontendApiUrl { get; set; }

        [StringLength(MaxUrlLength)]
        public string WebUrl { get; set; }

        [StringLength(DeveloperModel.MaxNameLength)]
        public string Name { get; set; }

        public bool? RequiresTermsAndConditions { get; set; }

        [StringLength(DeveloperModel.MaxLogoUrlLength)]
        public string LogoSvgUrl { get; set; }

        [StringLength(DeveloperModel.MaxLogoUrlLength)]
        public string LogoPngUrl { get; set; }

        [StringLength(DeveloperModel.MaxLogoUrlLength)]
        public string IconIcoUrl { get; set; }

        public string StorageImplementation { get; set; }
        public string StorageConnectionString { get; set; }

        public int? PrimaryColor { get; set; }
        public int? SecondaryColor { get; set; }
        public int? TextOnPrimaryColor { get; set; }
        public int? TextOnSecondaryColor { get; set; }

        public string SupportEmail { get; set; }
        public string NoreplyEmail { get; set; }

        public string CustomInvitationEmailTextPrefix { get; set; }
        public string CustomInvitationEmailTextSuffix { get; set; }

        public string ProductName { get; set; }

        public string DefaultCulture { get; set; }
        public string DefaultCurrency { get; set; }

        public string ExternalAuthenticationMethod { get; set; }

        public string OidcClientId { get; set; }
        public string OidcClientSecret { get; set; }

        public string OidcEndpointUrl { get; set; }

        public string SamlEntityId { get; set; }

        public string SamlPeerEntityId { get; set; }

        public string SamlPeerIdpMetadataLocation { get; set; }
        public string SamlPeerIdpMetadata { get; set; }

        public string SamlCertificate { get; set; }

        public bool? SamlAllowWeakSigningAlgorithm { get; set; }

        public string ExternalDirectoryType { get; set; }
        public string ExternalDirectoryHost { get; set; }
        public int? ExternalDirectoryPort { get; set; }

        public bool? ExternalDirectoryUsesSsl { get; set; }

        public string ExternalDirectorySslCertificate { get; set; }

        public string ExternalDirectoryAccountDistinguishedName { get; set; }

        public string ExternalDirectoryPassword { get; set; }

        public string ExternalDirectoryLoginAttribute { get; set; }

        public string ExternalDirectoryBaseContexts { get; set; }

        public string ExternalDirectoryUserFilter { get; set; }
        public string ExternalDirectoryGroupFilter { get; set; }

        public int? ExternalDirectorySyncIntervalSeconds { get; set; }

        public string ExternalDirectoryAdministratorGroupUuid { get; set; }

        public bool ExternalUsersAreManuallyManaged { get; set; }

        public string CustomTenantSettingsJson { get; set; }

        public int Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }
    }
}
