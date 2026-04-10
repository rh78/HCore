using System.Collections.Immutable;
using System.Linq;
using HCore.Identity.Models;
using Newtonsoft.Json;
using OpenIddict.Abstractions;

namespace HCore.Identity.Extensions
{
    public static class OpenIddictExtensions
    {
        public static void SetClaimsSettings(this OpenIddictApplicationDescriptor openIddictApplicationDescriptor, Duende.IdentityServer.EntityFramework.Entities.Client identityServerClient)
        {
            var clientClaims = identityServerClient?.Claims?.ToDictionary(claim => $"{identityServerClient.ClientClaimsPrefix}{claim.Type}", claim => claim.Value);

            if (clientClaims == null || !clientClaims.Any())
            {
                return;
            }

            var claimsSettingsModel = new ClaimsSettingsModel()
            {
                ClientClaims = clientClaims
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
