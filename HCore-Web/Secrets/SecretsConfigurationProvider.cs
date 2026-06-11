using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using HCore.Storage.Helpers;
using Microsoft.Extensions.Configuration;

namespace HCore.Web.Secrets
{
    internal class SecretsConfigurationProvider : ConfigurationProvider
    {
        private readonly string _environment;

        public SecretsConfigurationProvider() : base()
        {
            _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLowerInvariant();
        }

        public override void Load()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{_environment}.json", optional: true, reloadOnChange: false)
                .Build();

            var secretsManagerConnectionString = configuration["SecretsManager:ConnectionString"];

            var secretsManagerServiceContext = configuration["SecretsManager:ServiceContext"]?.ToLower();

            if (string.IsNullOrEmpty(secretsManagerServiceContext))
            {
                throw new Exception("Secrets manager service context is missing");
            }

            IAmazonSecretsManager secretsManager;

            if (!string.IsNullOrEmpty(secretsManagerConnectionString)) 
            {
                var connectionInfoByKey = AwsHelpers.GetConnectionInfoByKey(secretsManagerConnectionString);

                secretsManager = AwsHelpers.GetSecretsManager(connectionInfoByKey);
            }
            else
            {
                secretsManager = new AmazonSecretsManagerClient();
            }

            var environmentPrefix = $"{_environment}/";
            var serviceContextEnvironmentPrefix = $"{secretsManagerServiceContext}/{_environment}/";

            var listSecretsTask = secretsManager.ListSecretsAsync(new ListSecretsRequest()
            {
                Filters = new List<Filter>()
                {
                    new Filter()
                    {
                        Key = "name",
                        Values = [ environmentPrefix, serviceContextEnvironmentPrefix ]
                    }
                }
            });

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            listSecretsTask.Wait();

            var listSecretsResponse = listSecretsTask.Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            var data = new Dictionary<string, string>();

            foreach (var secretListEntry in listSecretsResponse.SecretList)
            {
                var getSecretValueTask = secretsManager.GetSecretValueAsync(new GetSecretValueRequest()
                {
                    SecretId = secretListEntry.Name
                });

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                getSecretValueTask.Wait();

                var getSecretValueResponse = getSecretValueTask.Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

                var name = secretListEntry.Name;

                name = name.Replace(serviceContextEnvironmentPrefix, "");
                name = name.Replace(environmentPrefix, "");
                name = name.Replace("/", ":");

                data.Add(name, DecodeString(getSecretValueResponse));
            }
        }

        // based on https://docs.aws.amazon.com/code-library/latest/ug/secrets-manager_example_secrets-manager_GetSecretValue_section.html

        public static string DecodeString(GetSecretValueResponse response)
        {
            // Decrypts secret using the associated AWS Key Management Service
            // Customer Master Key (CMK.) Depending on whether the secret is a
            // string or binary value, one of these fields will be populated.

            if (response.SecretString is not null)
            {
                var secret = response.SecretString;

                return secret;
            }

            else if (response.SecretBinary is not null)
            {
                var memoryStream = response.SecretBinary;

                using StreamReader reader = new StreamReader(memoryStream);
                
                string decodedBinarySecret = Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));

                return decodedBinarySecret;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}