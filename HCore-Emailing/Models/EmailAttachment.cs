namespace HCore.Emailing.Models
{
    public class EmailAttachment
    {
        public string MimeType { get; set; }
        public string FileName { get; set; }
        public byte[] Content { get; set; }

        public EmailAttachment(string mimeType, string fileName, byte[] content)
        {
            MimeType = mimeType;
            FileName = fileName;
            Content = content;
        }
    }
}
