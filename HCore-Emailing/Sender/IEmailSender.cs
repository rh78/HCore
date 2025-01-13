using HCore.Emailing.Models;
using HCore.Tenants.Database.SqlServer.Models.Impl;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCore.Emailing.Sender
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string configurationKey, SmtpEmailSenderConfigurationModel emailSenderConfiguration, string fromOverride, string fromReplyToOverride, string fromDisplayNameOverride, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage, List<EmailAttachment> emailAttachments = null, bool allowFallback = true);
    }
}
