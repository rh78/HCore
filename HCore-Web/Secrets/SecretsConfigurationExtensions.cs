using Microsoft.Extensions.Configuration;

namespace HCore.Web.Secrets
{
    public static class SecretsConfigurationExtensions
    {
        public static IConfigurationBuilder AddAmazonSecretsManager(this IConfigurationBuilder builder)
        {
            builder.Add(new SecretsConfigurationSource());

            return builder;
        }
    }
}
