using Newtonsoft.Json;

namespace HCore.Identity.Models
{
    public class OAuthProtectedResourceModel
    {
        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("authorization_servers")]
        public string[] AuthorizationServers { get; set; }

        [JsonProperty("scopes_supported", NullValueHandling = NullValueHandling.Ignore)]
        public string[] ScopesSupported { get; set; }
    }
}
