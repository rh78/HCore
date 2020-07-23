using HCore.Emailing.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace HCore.Emailing.Sender.Impl
{
    public class DirectEmailSenderImpl : EmailSenderImpl, IEmailSender
    {
        private readonly ILogger<DirectEmailSenderImpl> _logger;

        private readonly IConfiguration _configuration;

        private readonly Dictionary<string, SmtpEmailSenderConfiguration> _smtpEmailSenderConfigurations = new Dictionary<string, SmtpEmailSenderConfiguration>();
        private readonly Dictionary<string, SendGridEmailSenderConfiguration> _sendGridEmailSenderConfigurations = new Dictionary<string, SendGridEmailSenderConfiguration>();

        private readonly bool _useSendGrid;

        public DirectEmailSenderImpl(ILogger<DirectEmailSenderImpl> logger, IConfiguration configuration)
        {
            _configuration = configuration;

            string implementation = configuration["Emailing:Implementation"];

            if (string.IsNullOrEmpty(implementation))
                throw new Exception("Emailing implementation specification is empty");

            _useSendGrid = string.Equals(implementation, "SendGrid");            

            _logger = logger;
        }

        public async Task SendEmailAsync(string configurationKey, string fromOverride, string fromDisplayNameOverride, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage, List<EmailAttachment> emailAttachments = null)
        {
            _logger.LogInformation($"Sending email: {subject}");

            try
            {
                if (_useSendGrid)
                    await SendSendGridEmailAsync(configurationKey, fromOverride, fromDisplayNameOverride, to, cc, bcc, subject, htmlMessage, emailAttachments).ConfigureAwait(false);
                else
                    await SendSmtpEmailAsync(configurationKey, fromOverride, fromDisplayNameOverride, to, cc, bcc, subject, htmlMessage, emailAttachments).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error sending e-mail: {e}");

                throw e;
            }
        }
        
        private async Task SendSmtpEmailAsync(string configurationKey, string fromOverride, string fromDisplayNameOverride, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage, List<EmailAttachment> emailAttachments = null)
        {            
            if (string.IsNullOrEmpty(configurationKey))
                configurationKey = EmailSenderConstants.EmptyConfigurationKeyDefaultKey;
                    
            if (!_smtpEmailSenderConfigurations.ContainsKey(configurationKey))
                _smtpEmailSenderConfigurations.Add(configurationKey, LoadSmtpEmailSenderConfiguration(configurationKey, _configuration));

            SmtpEmailSenderConfiguration emailSenderConfiguration = _smtpEmailSenderConfigurations[configurationKey];

            using (SmtpClient client = new SmtpClient(emailSenderConfiguration.SmtpHost)) {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(emailSenderConfiguration.SmtpUserName, emailSenderConfiguration.SmtpPassword);
                client.Port = emailSenderConfiguration.SmtpPort;
                client.EnableSsl = emailSenderConfiguration.SmtpEnableSsl;

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(!string.IsNullOrEmpty(fromOverride) ? fromOverride : emailSenderConfiguration.SmtpEmailAddress,
                                                   !string.IsNullOrEmpty(fromDisplayNameOverride) ? fromDisplayNameOverride : emailSenderConfiguration.SmtpFromDisplayName);                    

                if (to != null)
                    to.ForEach(toString => mailMessage.To.Add(toString));

                if (cc != null)
                    cc.ForEach(ccString => mailMessage.CC.Add(ccString));

                if (bcc != null)
                    bcc.ForEach(bccString => mailMessage.Bcc.Add(bccString));

                mailMessage.IsBodyHtml = true;

                mailMessage.Subject = subject;
                mailMessage.SubjectEncoding = Encoding.UTF8;

                mailMessage.Body = htmlMessage;

                if (emailAttachments != null)
                {
                    foreach (var emailAttachment in emailAttachments)
                    {
                        var memoryStream = new MemoryStream(emailAttachment.Content);
                        System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(memoryStream, emailAttachment.FileName, emailAttachment.MimeType);

                        mailMessage.Attachments.Add(attachment);
                    }
                }

                await client.SendMailAsync(mailMessage).ConfigureAwait(false);
            }            
        }

        private async Task SendSendGridEmailAsync(string configurationKey, string fromOverride, string fromDisplayNameOverride, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage, List<EmailAttachment> emailAttachments = null)
        {            
            if (string.IsNullOrEmpty(configurationKey))
                configurationKey = EmailSenderConstants.EmptyConfigurationKeyDefaultKey;

            if (!_sendGridEmailSenderConfigurations.ContainsKey(configurationKey))
                _sendGridEmailSenderConfigurations.Add(configurationKey, LoadSendGridEmailSenderConfiguration(configurationKey, _configuration));

            SendGridEmailSenderConfiguration emailSenderConfiguration = _sendGridEmailSenderConfigurations[configurationKey];

            var client = new SendGridClient(emailSenderConfiguration.ApiKey);

            var mailMessage = new SendGridMessage()
            {
                From = new EmailAddress(!string.IsNullOrEmpty(fromOverride) ? fromOverride : emailSenderConfiguration.FromEmailAddress,
                                        !string.IsNullOrEmpty(fromDisplayNameOverride) ? fromDisplayNameOverride : emailSenderConfiguration.FromDisplayName),
                Subject = subject,                    
                HtmlContent = htmlMessage
            };
                
            if (to != null)
                to.ForEach(toString => mailMessage.AddTo(new EmailAddress(toString)));

            if (cc != null)
                cc.ForEach(ccString => mailMessage.AddCc(new EmailAddress(ccString)));

            if (bcc != null)
                bcc.ForEach(bccString => mailMessage.AddBcc(new EmailAddress(bccString)));

            if (emailAttachments != null)
            {
                foreach (var emailAttachment in emailAttachments)
                {
                    mailMessage.AddAttachment(new SendGrid.Helpers.Mail.Attachment()
                    {
                        Content = Convert.ToBase64String(emailAttachment.Content),
                        Filename = emailAttachment.FileName,
                        Type = emailAttachment.MimeType
                    });
                }
            }

            var response = await client.SendEmailAsync(mailMessage).ConfigureAwait(false);

            // see https://github.com/sendgrid/sendgrid-csharp

            // "After executing the above code, response.StatusCode should be 202 and you should have an email in the inbox of the to recipient."

            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                string body = await response.Body.ReadAsStringAsync().ConfigureAwait(false);

                throw new Exception($"SendGrid email sending failed with status code {response.StatusCode}: {body}");
            }                               
        }

        internal class SendGridEmailSenderConfiguration
        {
            public string ApiKey { get; set; }
            public string FromEmailAddress { get; set; }
            public string FromDisplayName { get; set; }
        }

        private SendGridEmailSenderConfiguration LoadSendGridEmailSenderConfiguration(string configurationKey, IConfiguration configuration)
        {
            string apiKey = configuration[$"Emailing:SendGrid:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new Exception($"SendGrid API key is missing");

            string fromEmailAddress = configuration[$"Emailing:SendGrid:{configurationKey}:EmailAddress"];
            if (string.IsNullOrEmpty(fromEmailAddress))
                throw new Exception($"SendGrid from email address is missing for key {configurationKey}");

            string fromDisplayName = configuration[$"Emailing:SendGrid:{configurationKey}:DisplayName"];
            if (string.IsNullOrEmpty(fromDisplayName))
                throw new Exception($"SendGrid from display name is missing for key {configurationKey}");

            return new SendGridEmailSenderConfiguration()
            {
                ApiKey = apiKey,
                FromEmailAddress = fromEmailAddress,
                FromDisplayName = fromDisplayName
            };
        }

        internal class SmtpEmailSenderConfiguration
        {
            public string SmtpEmailAddress { get; set; }
            public string SmtpFromDisplayName { get; set; }
            public string SmtpHost { get; set; }
            public string SmtpUserName { get; set; }
            public string SmtpPassword { get; set; }
            public int SmtpPort { get; set; }
            public bool SmtpEnableSsl { get; set; }
        }

        private SmtpEmailSenderConfiguration LoadSmtpEmailSenderConfiguration(string configurationKey, IConfiguration configuration)
        {
            string smtpEmailAddress = configuration[$"Smtp:{configurationKey}:EmailAddress"];
            if (string.IsNullOrEmpty(smtpEmailAddress))
                throw new Exception($"SMTP email address is missing for key {configurationKey}");

            string smtpFromDisplayName = configuration[$"Smtp:{configurationKey}:DisplayName"];
            if (string.IsNullOrEmpty(smtpFromDisplayName))
                throw new Exception($"SMTP from display name is missing for key {configurationKey}");

            string smtpHost = configuration[$"Smtp:{configurationKey}:Host"];
            if (string.IsNullOrEmpty(smtpHost))
                throw new Exception($"SMTP host is missing for key {configurationKey}");

            string smtpUserName = configuration[$"Smtp:{configurationKey}:UserName"];
            if (string.IsNullOrEmpty(smtpUserName))
                throw new Exception($"SMTP user name is missing for key {configurationKey}");

            string smtpPassword = configuration[$"Smtp:{configurationKey}:Password"];
            if (string.IsNullOrEmpty(smtpPassword))
                throw new Exception($"SMTP password is missing for key {configurationKey}");

            int smtpPort = configuration.GetValue<int>($"Smtp:{configurationKey}:Port");
            if (smtpPort <= 0)
                throw new Exception($"SMTP port is missing for key {configurationKey}");

            bool smtpEnableSsl = configuration.GetValue<bool>($"Smtp:{configurationKey}:EnableSsl");

            return new SmtpEmailSenderConfiguration()
            {
                SmtpEmailAddress = smtpEmailAddress,
                SmtpFromDisplayName = smtpFromDisplayName,
                SmtpHost = smtpHost,
                SmtpUserName = smtpUserName,
                SmtpPassword = smtpPassword,
                SmtpPort = smtpPort,
                SmtpEnableSsl = smtpEnableSsl
            };
        }        
    }
}
