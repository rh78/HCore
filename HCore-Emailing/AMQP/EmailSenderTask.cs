using HCore.Amqp;
using System.Collections.Generic;

namespace HCore.Emailing.AMQP
{
    internal class EmailSenderTask : AMQPMessage
    {
        public EmailSenderTask() 
            : base(EmailSenderConstants.ActionSend)
        {
        }

        public string ConfigurationKey { get; set; }

        public string FromOverride { get; set; }

        public List<string> To { get; set; }
        public List<string> Cc { get; set; }
        public List<string> Bcc { get; set; }

        public string Subject { get; set; }
        public string HtmlMessage { get; set; }
    }
}
