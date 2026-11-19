using System.Collections.Generic;
using System.Threading.Tasks;
using HCore.Amqp.Message;
using HCore.Amqp.Messenger;
using HCore.Identity.Services.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HCore.Test.Identity
{
    public class IdentityChangeMessageGroupTests
    {
        private const string IdentityChangeTasksAddress = "IdentityChangeTasks";
        private const string UserUuid = "user-1";

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task IdentityChange_UsesTheUserAsMessageGroup(bool trySynchronousFirst)
        {
            var messageGroupIds = new List<string>();

            var amqpMessenger = new Mock<IAMQPMessenger>();

            amqpMessenger
                .Setup(messenger => messenger.SendMessageAsync(IdentityChangeTasksAddress, It.IsAny<AMQPMessage>(), It.IsAny<double?>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, AMQPMessage, double?, string, string>((_, _, _, _, messageGroupId) => messageGroupIds.Add(messageGroupId))
                .Returns(Task.CompletedTask);

            amqpMessenger
                .Setup(messenger => messenger.SendMessageTrySynchronousFirstAsync(IdentityChangeTasksAddress, It.IsAny<AMQPMessage>(), It.IsAny<double?>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, AMQPMessage, double?, string, string>((_, _, _, _, messageGroupId) => messageGroupIds.Add(messageGroupId))
                .Returns(Task.CompletedTask);

            var identityServices = CreateIdentityServices(amqpMessenger.Object);

            await identityServices.SendUserChangeNotificationAsync(UserUuid, trySynchronousFirst: trySynchronousFirst);

            Assert.Equal(new[] { UserUuid }, messageGroupIds);
        }

        private static IdentityServicesImpl CreateIdentityServices(IAMQPMessenger amqpMessenger)
        {
            var configurationProvider = new Mock<HCore.Identity.Providers.IConfigurationProvider>();

            configurationProvider.SetupGet(provider => provider.IdentityChangeTasksAmqpAddress).Returns(IdentityChangeTasksAddress);

            var services = new ServiceCollection();

            services.AddSingleton(amqpMessenger);

            // sending the notification needs only the configuration provider and the messenger
            return new IdentityServicesImpl(
                signInManager: null,
                userManager: null,
                logger: null,
                emailSender: null,
                emailTemplateProvider: null,
                urlHelper: null,
                identityDbContext: null,
                configurationProvider.Object,
                restSharpClientProvider: null,
                nowProvider: null,
                services.BuildServiceProvider(),
                new ConfigurationBuilder().Build());
        }
    }
}
