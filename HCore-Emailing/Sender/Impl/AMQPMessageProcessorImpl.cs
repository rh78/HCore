using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using HCore.Amqp;
using HCore.Emailing.AMQP;

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

            await SendEmailAsync(emailSenderTask.ConfigurationKey, emailSenderTask.FromOverride, emailSenderTask.To, emailSenderTask.Cc, emailSenderTask.Bcc, emailSenderTask.Subject, emailSenderTask.HtmlMessage).ConfigureAwait(false);
            
            return true;
        }
    }
}
