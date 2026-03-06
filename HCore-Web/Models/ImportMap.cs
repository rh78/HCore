using System.Collections.Generic;
using Newtonsoft.Json;

namespace HCore.Web.Models
{
    internal class ImportMap
    {
        [JsonProperty("imports")]
        public Dictionary<string, string> Imports { get; set; }
    }
}
