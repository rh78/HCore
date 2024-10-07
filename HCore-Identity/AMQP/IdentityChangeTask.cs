using HCore.Amqp.Message;

namespace HCore.Identity.Amqp
{
    public class IdentityChangeTask : AMQPMessage
    {
        public IdentityChangeTask() 
            : base(IdentityCoreConstants.ActionNotify)
        {
        }

        public long? DeveloperUuid { get; set; }
        public long? TenantUuid { get; set; }

        public string UserUuid { get; set; }

        public string AccessTokenCache { get; set; }
        public string RefreshTokenCache { get; set; }
        public bool IsRegistration { get; set; }

        public string IdentifyAnonymousUserUuid { get; set; }
    }
}
