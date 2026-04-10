using System.Collections.Generic;
using Newtonsoft.Json;

namespace HCore.Identity.Models
{
    public class ClaimsSettingsModel
    {
        [JsonProperty("client_claims")]
        public Dictionary<string, string> ClientClaims { get; set; }
    }
}
