﻿using HCore.Amqp.Message;
using HCore.Tenants.Database.SqlServer.Models.Impl;
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

        public SmtpEmailSenderConfigurationModel EmailSenderConfiguration { get; set; }

        public string FromOverride { get; set; }
        public string FromReplyToOverride { get; set; }
        public string FromDisplayNameOverride { get; set; }

        public List<string> To { get; set; }
        public List<string> Cc { get; set; }
        public List<string> Bcc { get; set; }

        public string Subject { get; set; }
        public string HtmlMessage { get; set; }

        public List<EmailSenderTaskEmailAttachment> EmailAttachments { get; set; }

        public bool AllowFallback { get; set; }
    }
}
