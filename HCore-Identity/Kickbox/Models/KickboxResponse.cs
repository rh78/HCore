using Newtonsoft.Json;

namespace HCore.Identity.Kickbox.Models
{
    public class KickboxResponse
    {
        [JsonProperty("result")]
        public KickboxResultEnum Result { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("role")]
        public bool Role { get; set; }

        [JsonProperty("free")]
        public bool Free { get; set; }

        [JsonProperty("disposable")]
        public bool Disposable { get; set; }

        [JsonProperty("accept_all")]
        public bool AcceptAll { get; set; }

        [JsonProperty("did_you_mean")]
        public string DidYouMean { get; set; }

        [JsonProperty("sendex")]
        public double Sendex { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }
    }
}
