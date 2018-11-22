namespace HCore.Emailing.AMQP
{
    internal class EmailSenderTaskEmailAttachment
    {
        public string MimeType { get; set; }
        public string FileName { get; set; }
        public string Base64EncodedContent { get; set; }
    }
}
