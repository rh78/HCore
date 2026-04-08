using System.Collections.Generic;
using Newtonsoft.Json;

namespace HCore.Identity.Models
{
    public class ClaimsSettingsModel
    {
        [JsonProperty("claims")]
        public Dictionary<string, string> Claims { get; set; }
    }
}
