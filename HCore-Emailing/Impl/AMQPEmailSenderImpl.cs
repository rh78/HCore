using HCore.Amqp;
using HCore.Amqp.Processor;
using HCore.Emailing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HCore.Emailing.Impl
{
    internal class AMQPEmailSenderImpl : EmailSenderImpl, IEmailSender
    {
        private readonly IAMQPMessenger _amqpMessenger;

        public AMQPEmailSenderImpl(IAMQPMessenger amqpMessenger)            
        {
            _amqpMessenger = amqpMessenger;
        }

        public async Task SendEmailAsync(string configurationKey, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage)
        {
            var emailSenderTask = new EmailSenderTask()
            {
                ConfigurationKey = configurationKey,
                To = to,
                Cc = cc,
                Bcc = bcc,
                Subject = subject,
                HtmlMessage = htmlMessage
            };
        
            await _amqpMessenger.SendMessageAsync(EmailSenderConstants.Address, 
                emailSenderTask).ConfigureAwait(false);         
        }
    }
}
