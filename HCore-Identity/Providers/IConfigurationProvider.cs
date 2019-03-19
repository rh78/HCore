namespace HCore.Identity.Providers
{
    public interface IConfigurationProvider
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

        string PrivacyPolicyUrl { get; }
        int PrivacyPolicyVersion { get; }

        bool RequiresTermsAndConditions { get; }
        string TermsAndConditionsUrl { get; }
        int TermsAndConditionsVersion { get; }   
        
        string ProductName { get; }
    }
}
