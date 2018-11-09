using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace HCore.Identity.Providers.Impl
{
    internal class ConfigurationProviderImpl : IConfigurationProvider
    {
        public string DefaultClientId { get; private set; }
        public string DefaultClientAuthority { get; private set; }
        public string DefaultClientAudience { get; private set; }

        public bool SelfRegistration { get; private set; }
        public bool RegisterName { get; private set; }
        public bool RegisterPhoneNumber { get; private set; }

        public bool SelfManagement { get; private set; }
        public bool ManageName { get; private set; }
        public bool ManagePhoneNumber { get; private set; }

        public bool RequireEmailConfirmed { get; private set; }

        public string IdentityChangeTasksAmqpAddress { get; private set; }
        
        public ConfigurationProviderImpl(IConfiguration configuration)
        {
            DefaultClientId = configuration[$"Identity:DefaultClient:ClientId"];
            if (string.IsNullOrEmpty(DefaultClientId))
                throw new Exception("Identity default client ID is empty");

            DefaultClientAuthority = configuration[$"Identity:DefaultClient:Authority"];
            if (string.IsNullOrEmpty(DefaultClientAuthority))
                throw new Exception("Identity default client authority string is empty");

            DefaultClientAudience = configuration[$"Identity:DefaultClient:Audience"];
            if (string.IsNullOrEmpty(DefaultClientAudience))
                throw new Exception("Identity default client audience string is empty");

            SelfRegistration = configuration.GetValue<bool>("Identity:FeatureSet:SelfRegistration");
            RegisterName = configuration.GetValue<bool>("Identity:FeatureSet:RegisterName");
            RegisterPhoneNumber = configuration.GetValue<bool>("Identity:FeatureSet:RegisterPhoneNumber");

            SelfManagement = configuration.GetValue<bool>("Identity:FeatureSet:SelfManagement");
            ManageName = configuration.GetValue<bool>("Identity:FeatureSet:ManageName");
            ManagePhoneNumber = configuration.GetValue<bool>("Identity:FeatureSet:ManagePhoneNumber");

            RequireEmailConfirmed = configuration.GetValue<bool>("Identity:FeatureSet:RequireEmailConfirmed");

            string amqpAddresses = configuration["Amqp:Addresses"];

            if (!string.IsNullOrEmpty(amqpAddresses))
            {
                string[] amqpAddressesSplit = amqpAddresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                IdentityChangeTasksAmqpAddress = amqpAddressesSplit.FirstOrDefault(address => address.EndsWith(IdentityCoreConstants.IdentityChangeTasksAddressSuffix));
            }
        }
    }
}
