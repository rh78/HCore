namespace HCore.Identity
{
    public class IdentityCoreConstants
    {
        public const string OidcScheme = "oidc";
        public const string JwtScheme = "jwt";

        public const string ExternalOidcScheme = "oidc.external";

        public const string JwtPolicy = "JwtPolicy";

        public const string DeveloperUuidClientClaim = "client_developer_uuid";

        public const string DeveloperAdminClaim = "developer_admin";
        public const string DeveloperAdminClientClaim = "client_developer_admin";

        public const string IdentityChangeTasksAddressSuffix = "IdentityChangeTasks";

        public const string ActionNotify = "notify";

        public const int AccessTokenValidityInSeconds = 3600;

        public const string UuidSeparator = ":";
    }
}
