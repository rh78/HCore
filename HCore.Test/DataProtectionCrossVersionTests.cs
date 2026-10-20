using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HCore.Test
{
    public class DataProtectionCrossVersionTests
    {
        private const string KeyFixtureFileName = "data-protection-key.xml";
        private const string PayloadFixtureFileName = "data-protection-payload.txt";

        private const string ApplicationName = "HCore.Test";
        private const string Purpose = "HCore.Test.CrossVersion";
        private const string Plaintext = "HCore Data Protection fixture";

        [Fact]
        public void Net8Payload_UnprotectsWithTheNet8KeyRing()
        {
            var keyFixturePath = FixtureFile.GetPath(KeyFixtureFileName);
            var payloadFixturePath = FixtureFile.GetPath(PayloadFixtureFileName);

            var xmlRepository = new InMemoryXmlRepository();

            if (!File.Exists(keyFixturePath))
            {
                var captureDataProtector = CreateDataProtector(xmlRepository);

                var protectedPayload = captureDataProtector.Protect(Plaintext);

                Directory.CreateDirectory(Path.GetDirectoryName(keyFixturePath));

                var keyElement = xmlRepository
                    .GetAllElements()
                    .Single();

                keyElement.Save(keyFixturePath);

                File.WriteAllText(payloadFixturePath, protectedPayload);

                Assert.Fail($"No fixtures yet. Captured {keyFixturePath} and {payloadFixturePath}, commit them only from the .NET 8 build.");
            }

            var fixtureKeyElement = XElement.Load(keyFixturePath);

            xmlRepository.StoreElement(fixtureKeyElement, KeyFixtureFileName);

            var dataProtector = CreateDataProtector(xmlRepository);

            var fixturePayload = File.ReadAllText(payloadFixturePath);

            var plaintext = dataProtector.Unprotect(fixturePayload);

            Assert.Equal(Plaintext, plaintext);
        }

        // set up like PersistKeysToSqlDatabase in HCore-Identity, with the key ring in memory instead of SQL
        private static IDataProtector CreateDataProtector(InMemoryXmlRepository xmlRepository)
        {
            var services = new ServiceCollection();

            services.AddDataProtection()
                .SetApplicationName(ApplicationName);

            services.Configure<KeyManagementOptions>(options => options.XmlRepository = xmlRepository);

            var serviceProvider = services.BuildServiceProvider();

            var dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();

            return dataProtectionProvider.CreateProtector(Purpose);
        }
    }
}
