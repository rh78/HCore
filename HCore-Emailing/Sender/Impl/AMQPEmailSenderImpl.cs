using HCore.Amqp.Messenger;
using HCore.Emailing.AMQP;
using HCore.Emailing.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HCore.Emailing.Sender.Impl
{
    internal class AMQPEmailSenderImpl : EmailSenderImpl, IEmailSender
    {
        private readonly IAMQPMessenger _amqpMessenger;

        private readonly string _emailSenderAddress;

        public AMQPEmailSenderImpl(IAMQPMessenger amqpMessenger, IConfiguration configuration)            
        {
            _amqpMessenger = amqpMessenger;

            string addresses = configuration["Amqp:Addresses"];

            if (string.IsNullOrEmpty(addresses))
                throw new Exception("AMQP addresses are missing");

            string[] addressesSplit = addresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            _emailSenderAddress = addressesSplit.FirstOrDefault(address => address.EndsWith(EmailSenderConstants.AddressSuffix));

            if (_emailSenderAddress == null)
                throw new Exception($"AMQP email sender requires the AMQP address suffix {EmailSenderConstants.AddressSuffix}' to be defined");
        }

        public async Task SendEmailAsync(string configurationKey, string fromOverride, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage, List<EmailAttachment> emailAttachments = null)
        {
            if (to.TrueForAll(string.IsNullOrEmpty))
                throw new Exception("At least one valid to address is required!");

            long totalApproximateSize = 0;

            var emailSenderTask = new EmailSenderTask()
            {
                ConfigurationKey = configurationKey,
                FromOverride = fromOverride,
                To = to,
                Cc = cc,
                Bcc = bcc,
                Subject = subject,
                HtmlMessage = htmlMessage
            };

            totalApproximateSize += htmlMessage.Length;

            if (emailAttachments != null)
            {
                emailSenderTask.EmailAttachments = new List<EmailSenderTaskEmailAttachment>();

                foreach (var emailAttachment in emailAttachments)
                {
                    var base64EncodedContent = Convert.ToBase64String(emailAttachment.Content);

                    totalApproximateSize += base64EncodedContent.Length;

                    emailSenderTask.EmailAttachments.Add(new EmailSenderTaskEmailAttachment()
                    {
                        Base64EncodedContent = base64EncodedContent,
                        FileName = emailAttachment.FileName,
                        MimeType = emailAttachment.MimeType
                    });
                }
            }

            if (totalApproximateSize > 200000)
            {
                // 256kB is Service Bus max message size

                throw new Exception("The total email message size must not exceed 200 kB");
            }
        
            await _amqpMessenger.SendMessageAsync(_emailSenderAddress, 
                emailSenderTask).ConfigureAwait(false);         
        }
    }
}
