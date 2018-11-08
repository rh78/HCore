namespace HCore.Identity
{
    public interface IIdentityServicesConfiguration
    {
        string DefaultClientId { get; }
        string DefaultClientAuthority { get; }
        string DefaultClientAudience { get; }

        bool SelfRegistration { get; }
        bool RegisterName { get; }
        bool RegisterPhoneNumber { get; }

        bool SelfManagement { get; }
        bool ManageName { get; }
        bool ManagePhoneNumber { get; }

        bool RequireEmailConfirmed { get; }

        string IdentityChangeTasksAmqpAddress { get; }
    }
}
