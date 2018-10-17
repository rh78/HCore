using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HCore.Emailing.Impl
{
    public class DirectEmailSenderImpl : EmailSenderImpl, IEmailSender
    {
        private readonly ILogger<DirectEmailSenderImpl> _logger;

        private readonly IConfiguration _configuration;

        private readonly Dictionary<string, EmailSenderConfiguration> _emailSenderConfigurations = new Dictionary<string, EmailSenderConfiguration>();

        public DirectEmailSenderImpl(ILogger<DirectEmailSenderImpl> logger, IConfiguration configuration)
        {
            _configuration = configuration;

            _logger = logger;
        }

        public async Task SendEmailAsync(string configurationKey, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage)
        {
            _logger.LogInformation($"Sending email: {subject}");

            try {
                if (string.IsNullOrEmpty(configurationKey))
                    configurationKey = EmailSenderConstants.EmptyConfigurationKeyDefaultKey;
                    
                if (!_emailSenderConfigurations.ContainsKey(configurationKey))
                    _emailSenderConfigurations.Add(configurationKey, LoadEmailSenderConfiguration(configurationKey, _configuration));

                EmailSenderConfiguration emailSenderConfiguration = _emailSenderConfigurations[configurationKey];

                using (SmtpClient client = new SmtpClient(emailSenderConfiguration.SmtpHost)) {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(emailSenderConfiguration.SmtpUserName, emailSenderConfiguration.SmtpPassword);
                    client.Port = emailSenderConfiguration.SmtpPort;
                    client.EnableSsl = emailSenderConfiguration.SmtpEnableSsl;

                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(emailSenderConfiguration.SmtpEmailAddress);
                    string user = mailMessage.From.User;

                    if (to != null)
                        to.ForEach(toString => mailMessage.To.Add(toString));

                    if (cc != null)
                        cc.ForEach(ccString => mailMessage.CC.Add(ccString));

                    if (bcc != null)
                        bcc.ForEach(bccString => mailMessage.Bcc.Add(bccString));

                    mailMessage.IsBodyHtml = true;

                    mailMessage.Subject = subject;
                    mailMessage.Body = htmlMessage;

                    await client.SendMailAsync(mailMessage).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error sending e-mail: {e}");

                throw e;
            }
        }

        internal static EmailSenderConfiguration LoadEmailSenderConfiguration(string configurationKey, IConfiguration configuration)
        {
            string smtpEmailAddress = configuration[$"Smtp:{configurationKey}:EmailAddress"];
            if (string.IsNullOrEmpty(smtpEmailAddress))
                throw new Exception($"SMTP email address is missing for key {configurationKey}");

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

            return new EmailSenderConfiguration()
            {
                SmtpEmailAddress = smtpEmailAddress,
                SmtpHost = smtpHost,
                SmtpUserName = smtpUserName,
                SmtpPassword = smtpPassword,
                SmtpPort = smtpPort,
                SmtpEnableSsl = smtpEnableSsl
            };
        }
    }
}
