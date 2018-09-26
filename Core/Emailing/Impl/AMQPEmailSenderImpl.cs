using ReinhardHolzner.Core.AMQP;
using ReinhardHolzner.Core.AMQP.Processor;
using ReinhardHolzner.Core.Emailing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Emailing.Impl
{
    internal class AMQPEmailSenderImpl : EmailSenderImpl, IEmailSender
    {
        private IAMQPMessenger _amqpMessenger;

        public AMQPEmailSenderImpl(IAMQPMessenger amqpMessenger)            
        {
            _amqpMessenger = amqpMessenger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailSenderTask = new EmailSenderTask()
            {
                To = new string[] { email }.ToList(),
                Subject = subject,
                HtmlMessage = htmlMessage
            };
        
            await _amqpMessenger.SendMessageAsync(EmailSenderConstants.Address, 
                emailSenderTask).ConfigureAwait(false);         
        }
    }
}
