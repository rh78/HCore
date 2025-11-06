using System.ComponentModel.DataAnnotations;

namespace HCore.Web.Models
{
    public class OpenTelemetryConfigurationModel
    {
        [Required]
        public string ServiceName { get; set; }

        public string ServiceVersion { get; set; }

        public string Protocol { get; set; }

        public string Endpoint { get; set; }

        public bool AddMetrics { get; set; }

        public bool AddTracing { get; set; }

        public bool AddLogging { get; set; }
    }
}
