using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReinhardHolzner.Core.AMQP;

namespace ReinhardHolzner.Core.Emailing.Impl
{
    public class AMQPMessageProcessorImpl : IAMQPMessageProcessor
    {
        private ILogger<AMQPMessageProcessorImpl> _logger;

        public AMQPMessageProcessorImpl(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AMQPMessageProcessorImpl>();
        }

#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        public virtual async Task<bool> ProcessMessageAsync(string address, string messageBodyJson)
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            if (!string.Equals(address, EmailSenderConstants.Address))
                return false;

            EmailSenderTask emailSenderTask = JsonConvert.DeserializeObject<EmailSenderTask>(messageBodyJson);

            _logger.LogInformation($"Sending AMQP email to {emailSenderTask.To}: {emailSenderTask.Subject}");

            return true;
        }
    }
}
