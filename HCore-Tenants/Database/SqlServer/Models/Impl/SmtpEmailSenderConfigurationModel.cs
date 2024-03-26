using System;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    [Serializable]
    public class SmtpEmailSenderConfigurationModel
    {
        public string SmtpEmailAddress { get; set; }
        public string SmtpFromDisplayName { get; set; }
        public string SmtpHost { get; set; }
        public string SmtpUserName { get; set; }
        public string SmtpPassword { get; set; }
        public int SmtpPort { get; set; }
        public bool SmtpEnableSsl { get; set; }
        public bool? SmtpStartTls { get; set; }
        public bool? SmtpEnableExtendedLogging { get; set; }
    }
}
