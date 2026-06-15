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
            if (Data.Any())
            {
                // already loaded

                return;
            }

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

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            var environmentDataTask = GetEnvironmentDataAsync(secretsManagerConnectionString, secretsManagerServiceContext);

            environmentDataTask.Wait();

            Data = environmentDataTask.Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }

        private async Task<Dictionary<string, string>> GetEnvironmentDataAsync(string secretsManagerConnectionString, string secretsManagerServiceContext)
        {
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

            string nextToken = null;

            var serviceContextEnvironmentPrefix = $"{_environment}/{secretsManagerServiceContext}/";

            var environmentPrefix = $"{_environment}/";

            var secretList = new List<SecretListEntry>();

            do
            {
                var listSecretsResponse = await secretsManager.ListSecretsAsync(new ListSecretsRequest()
                {
                    NextToken = nextToken,
                    MaxResults = 100,
                    Filters = new List<Filter>()
                    {
                        new Filter()
                        {
                            Key = "name",
                            Values = [ environmentPrefix ]
                        }
                    }
                }).ConfigureAwait(false);

                if (!listSecretsResponse.SecretList.Any())
                {
                    break;
                }

                secretList.AddRange(listSecretsResponse.SecretList);

                nextToken = listSecretsResponse.NextToken;

                if (string.IsNullOrEmpty(nextToken))
                {
                    break;
                }
            }
            while (true);

            var serviceContextData = new Dictionary<string, string>();

            var environmentData = new Dictionary<string, string>();

            foreach (var secretListEntry in secretList)
            {
                var getSecretValueResponse = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest()
                {
                    SecretId = secretListEntry.Name
                }).ConfigureAwait(false);

                var name = secretListEntry.Name;

                if (name.StartsWith(serviceContextEnvironmentPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Replace(serviceContextEnvironmentPrefix, "", StringComparison.OrdinalIgnoreCase);
                    name = name.Replace("/", ":");

                    serviceContextData.Add(name, DecodeString(getSecretValueResponse));
                }
                else if (name.StartsWith(environmentPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Replace(environmentPrefix, "", StringComparison.OrdinalIgnoreCase);
                    name = name.Replace("/", ":");

                    environmentData.Add(name, DecodeString(getSecretValueResponse));
                }
                else
                {
                    continue;
                }
            }

            foreach (var serviceContextDataKeyValuePair in serviceContextData)
            {
                // service context settings ALWAYS overwrite generic environment settings

                environmentData[serviceContextDataKeyValuePair.Key] = serviceContextDataKeyValuePair.Value;
            }

            return environmentData;
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