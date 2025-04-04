﻿using Google.Apis.Logging;
using HCore.Amqp.Messenger;
using HCore.Emailing.AMQP;
using HCore.Emailing.Models;
using HCore.Tenants.Database.SqlServer.Models.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        private readonly ILogger<AMQPEmailSenderImpl> _logger;

        public AMQPEmailSenderImpl(IAMQPMessenger amqpMessenger, IConfiguration configuration, ILogger<AMQPEmailSenderImpl> logger)            
        {
            _amqpMessenger = amqpMessenger;

            string addresses = configuration["Amqp:Addresses"];

            if (string.IsNullOrEmpty(addresses))
                throw new Exception("AMQP addresses are missing");

            string[] addressesSplit = addresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            _emailSenderAddress = addressesSplit.FirstOrDefault(address => address.EndsWith(EmailSenderConstants.AddressSuffix));

            if (_emailSenderAddress == null)
                throw new Exception($"AMQP email sender requires the AMQP address suffix {EmailSenderConstants.AddressSuffix}' to be defined");

            _logger = logger;
        }

        public async Task SendEmailAsync(string configurationKey, SmtpEmailSenderConfigurationModel emailSenderConfiguration, string fromOverride, string fromReplyToOverride, string fromDisplayNameOverride, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage, List<EmailAttachment> emailAttachments = null, bool allowFallback = true)
        {
            if (to.TrueForAll(string.IsNullOrEmpty))
                throw new Exception("At least one valid to address is required!");

            if (string.IsNullOrEmpty(subject))
                throw new Exception("A subject line is required!");

            var enableExtendedLogging = emailSenderConfiguration?.SmtpEnableExtendedLogging ?? false;

            if (enableExtendedLogging)
            {
                var firstTo = to?.FirstOrDefault();

                _logger.LogWarning($"AMQP email sender processing email from {fromOverride}, for {firstTo}, subject {subject}");
            }

            long totalApproximateSize = 0;

            var emailSenderTask = new EmailSenderTask()
            {
                ConfigurationKey = configurationKey,
                EmailSenderConfiguration = emailSenderConfiguration,
                FromOverride = fromOverride,
                FromReplyToOverride = fromReplyToOverride,
                FromDisplayNameOverride = fromDisplayNameOverride,
                To = to,
                Cc = cc,
                Bcc = bcc,
                Subject = subject,
                HtmlMessage = htmlMessage,
                AllowFallback = allowFallback
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
