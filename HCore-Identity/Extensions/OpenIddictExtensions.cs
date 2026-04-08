using System.Collections.Immutable;
using System.Linq;
using HCore.Identity.Models;
using Newtonsoft.Json;
using OpenIddict.Abstractions;

namespace HCore.Identity.Extensions
{
    public static class OpenIddictExtensions
    {
        public static void SetClaimsSettings(this OpenIddictApplicationDescriptor openIddictApplicationDescriptor, Duende.IdentityServer.EntityFramework.Entities.Client identityServerClient, bool isLegacyClientSecret)
        {
            var claims = identityServerClient?.Claims?.ToDictionary(claim => claim.Type, claim => claim.Value);

            if (claims == null || !claims.Any())
            {
                return;
            }

            var claimsSettingsModel = new ClaimsSettingsModel()
            {
                Claims = claims
            };

            var claimsSettingsJson = JsonConvert.SerializeObject(claimsSettingsModel);

            openIddictApplicationDescriptor.Settings.Add(IdentityCoreConstants.ClaimsSettings, claimsSettingsJson);
        }

        public static ClaimsSettingsModel GetClaimsSettings(this ImmutableDictionary<string, string> settings)
        {
            string claimsSettingsJson = null;

            settings?.TryGetValue(IdentityCoreConstants.ClaimsSettings, out claimsSettingsJson);

            if (string.IsNullOrEmpty(claimsSettingsJson))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<ClaimsSettingsModel>(claimsSettingsJson);
        }
    }
}
