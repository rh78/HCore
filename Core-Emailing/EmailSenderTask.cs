using ReinhardHolzner.Core.Amqp;
using System.Collections.Generic;

namespace ReinhardHolzner.Core.Emailing
{
    internal class EmailSenderTask : AMQPMessage
    {
        public EmailSenderTask() 
            : base(EmailSenderConstants.ActionSend)
        {
        }

        public string ConfigurationKey { get; set; }

        public List<string> To { get; set; }
        public List<string> Cc { get; set; }
        public List<string> Bcc { get; set; }

        public string Subject { get; set; }
        public string HtmlMessage { get; set; }
    }
}
