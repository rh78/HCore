using HCore.Emailing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCore.Emailing.Sender
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string configurationKey, string fromOverride, string fromDisplayNameOverride, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage, List<EmailAttachment> emailAttachments = null);
    }
}
