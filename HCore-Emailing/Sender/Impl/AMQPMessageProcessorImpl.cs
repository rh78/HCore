using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using HCore.Emailing.AMQP;
using HCore.Amqp.Processor;
using HCore.Emailing.Models;
using System.Collections.Generic;
using System;

namespace HCore.Emailing.Sender.Impl
{
    public class AMQPMessageProcessorImpl : DirectEmailSenderImpl, IAMQPMessageProcessor
    {
        public AMQPMessageProcessorImpl(ILogger<AMQPMessageProcessorImpl> logger, IConfiguration configuration)
            : base(logger, configuration)
        {
        }

        public virtual async Task<bool> ProcessMessageAsync(string address, string messageBodyJson)
        {
            if (!address.EndsWith(EmailSenderConstants.AddressSuffix))
                return false;

            EmailSenderTask emailSenderTask = JsonConvert.DeserializeObject<EmailSenderTask>(messageBodyJson);

            List<EmailAttachment> emailAttachments = null;

            if (emailSenderTask.EmailAttachments != null)
            {
                emailAttachments = new List<EmailAttachment>();

                foreach (var encodedEmailAttachment in emailSenderTask.EmailAttachments)
                {
                    emailAttachments.Add(new EmailAttachment(
                        encodedEmailAttachment.MimeType,
                        encodedEmailAttachment.FileName,
                        Convert.FromBase64String(encodedEmailAttachment.Base64EncodedContent)));
                }
            }

            await SendEmailAsync(emailSenderTask.ConfigurationKey, emailSenderTask.FromOverride, emailSenderTask.FromDisplayNameOverride, emailSenderTask.To, emailSenderTask.Cc, emailSenderTask.Bcc, emailSenderTask.Subject, emailSenderTask.HtmlMessage, emailAttachments).ConfigureAwait(false);
            
            return true;
        }
    }
}
