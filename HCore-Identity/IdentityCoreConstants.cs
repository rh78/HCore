namespace HCore.Identity
{
    public class IdentityCoreConstants
    {
        public const string OidcScheme = "oidc";
        public const string JwtScheme = "jwt";

        public const string ExternalOidcScheme = "oidc.external";
        public const string ExternalSamlScheme = "saml.external";

        public const string JwtPolicy = "JwtPolicy";

        public const string DeveloperUuidClaim = "developer_uuid";
        public const string DeveloperUuidClientClaim = "client_developer_uuid";
        
        public const string TenantUuidClaim = "tenant_uuid";
        public const string TenantUuidClientClaim = "client_tenant_uuid";

        public const string DeveloperAdminClaim = "developer_admin";
        public const string DeveloperAdminClientClaim = "client_developer_admin";

        public const string OemAdminClaim = "oem_admin";
        public const string OemAdminClientClaim = "client_oem_admin";

        public const string AnonymousUserClaim = "anonymous_user";
        public const string AnonymousUserClientClaim = "client_anonymous_user";

        public const string IdentityChangeTasksAddressSuffix = "IdentityChangeTasks";

        public const string ActionNotify = "notify";

        public const int AccessTokenValidityInSeconds = 3600;

        public const string UuidSeparator = ":";

        public const string HttpContextItemsIdTokenHint = "SmintIo:IdTokenHint";

        public const string AllowIFrameUrlContextKey = "SmintIo:AllowIFrameUrl";
    }
}
