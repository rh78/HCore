using Microsoft.Extensions.Configuration;

namespace HCore.Web.Secrets
{
    internal class SecretsConfigurationSource : IConfigurationSource
    {
        private static SecretsConfigurationProvider _secretsConfigurationProvider;

        public IConfigurationProvider Build(IConfigurationBuilder builder) => _secretsConfigurationProvider ??= new SecretsConfigurationProvider();
    }
}
