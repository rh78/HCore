using HCore.Amqp;

namespace HCore.Identity.AMQP
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
