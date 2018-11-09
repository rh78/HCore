using HCore.Amqp.Message;

namespace HCore.Identity.Amqp
{
    public class IdentityChangeTask : AMQPMessage
    {
        public IdentityChangeTask() 
            : base(IdentityCoreConstants.ActionNotify)
        {
        }

        public string UserUuid { get; set; }
    }
}
