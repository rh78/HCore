using HCore.Amqp.Processor;
using HCore.Emailing.AMQP;
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

        public async Task SendEmailAsync(string configurationKey, string fromOverride, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage)
        {
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
        
            await _amqpMessenger.SendMessageAsync(_emailSenderAddress, 
                emailSenderTask).ConfigureAwait(false);         
        }
    }
}
