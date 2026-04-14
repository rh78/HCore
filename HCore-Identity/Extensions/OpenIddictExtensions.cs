using System.Collections.Immutable;
using HCore.Identity.Models;
using Newtonsoft.Json;

namespace HCore.Identity.Extensions
{
    public static class OpenIddictExtensions
    {
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
