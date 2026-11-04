using Amazon;
using Microsoft.Extensions.Configuration;

namespace HCore.Web.Secrets
{
    public static class SecretsConfigurationExtensions
    {
        public static IConfigurationBuilder AddAmazonSecretsManager(this IConfigurationBuilder builder)
        {
            // V4 of the AWS SDK leaves collections null, HCore still relies on the empty collections of V3

            AWSConfigs.InitializeCollections = true;

            builder.Add(new SecretsConfigurationSource());

            return builder;
        }
    }
}
