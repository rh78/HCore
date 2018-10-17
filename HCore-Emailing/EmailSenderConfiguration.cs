namespace HCore.Emailing
{
    internal class EmailSenderConfiguration
    {
        public string SmtpEmailAddress { get; set; }
        public string SmtpHost { get; set; }
        public string SmtpUserName { get; set; }
        public string SmtpPassword { get; set; }
        public int SmtpPort { get; set; }
        public bool SmtpEnableSsl { get; set; }
    }
}
