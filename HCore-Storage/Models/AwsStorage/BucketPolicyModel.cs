using System.Collections.Generic;
using Newtonsoft.Json;

namespace HCore.Storage.Models.AwsStorage
{
    public class BucketPolicyModel
    {
        [JsonProperty("Version")]
        public string Version { get; set; }

        [JsonProperty("Statement")]
        public List<BucketPolicyStatementModel> Statement { get; set; }
    }
}
