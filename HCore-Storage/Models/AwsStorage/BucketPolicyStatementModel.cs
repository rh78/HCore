using Newtonsoft.Json;

namespace HCore.Storage.Models.AwsStorage
{
    public class BucketPolicyStatementModel
    {
        [JsonProperty("Sid")]
        public string Sid { get; set; }

        [JsonProperty("Effect")]
        public string Effect { get; set; }

        [JsonProperty("Principal")]
        public object Principal { get; set; }

        [JsonProperty("Action")]
        public object Action { get; set; }

        [JsonProperty("Resource")]
        public object Resource { get; set; }
    }
}
