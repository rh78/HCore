using Microsoft.Extensions.Configuration;
using HCore.Amqp.Processor;
using System;
using HCore.Amqp.Messenger;
using HCore.Amqp.Messenger.Impl;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AmqpServiceCollectionExtensions
    {
        public static IServiceCollection AddAmqp(this IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Initializing AMQP...");

            string implementation = configuration["Amqp:Implementation"];

            if (string.IsNullOrEmpty(implementation))
                throw new Exception("AMQP implementation specification is empty");

            bool useServiceBus = string.Equals(implementation, "ServiceBus");
            bool useActiveMq = string.Equals(implementation, "ActiveMq");

            string connectionString = configuration["Amqp:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("AMQP connection string is empty");

            string addresses = configuration["Amqp:Addresses"];

            if (string.IsNullOrEmpty(addresses))
                throw new Exception("AMQP addresses are missing");

            string[] addressesSplit = addresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (addressesSplit.Length == 0)
                throw new Exception("AMQP addresses are empty");

            var amqpListenerCounts = GetListenerCounts(configuration, addressesSplit);
            var amqpIsSessions = GetSessions(configuration, addressesSplit, useServiceBus, useActiveMq);

            var topicAddresses = configuration["Amqp:TopicAddresses"];

            if (string.IsNullOrEmpty(topicAddresses))
            {
                // Optional
                topicAddresses = string.Empty;
            }

            var topicAddressesSplit = topicAddresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var amqpTopicListenerCounts = GetListenerCounts(configuration, topicAddressesSplit);
            var amqpIsTopicSessions = GetSessions(configuration, topicAddressesSplit, useServiceBus, useActiveMq);

            services.AddSingleton(serviceProvider =>
            {
                IAMQPMessageProcessor messageProcessor = serviceProvider.GetRequiredService<IAMQPMessageProcessor>();

                IAMQPMessenger amqpMessenger;

                if (useServiceBus)
                {
                    // Service Bus

                    amqpMessenger = new ServiceBusMessengerImpl(connectionString, addressesSplit, topicAddressesSplit, amqpListenerCounts, amqpTopicListenerCounts, amqpIsSessions, amqpIsTopicSessions, messageProcessor, serviceProvider.GetRequiredService<ILogger<ServiceBusMessengerImpl>>());
                }
                else if (useActiveMq)
                {
                    amqpMessenger = new ActiveMqMessengerImpl(connectionString, addressesSplit, topicAddressesSplit, amqpListenerCounts, amqpTopicListenerCounts, amqpIsSessions, amqpIsTopicSessions, messageProcessor, serviceProvider.GetRequiredService<ILogger<ActiveMqMessengerImpl>>());
                }
                else
                {
                    // AMQP 1.0

                    amqpMessenger = new AMQP10MessengerImpl(connectionString, addressesSplit, amqpListenerCounts, messageProcessor, serviceProvider.GetRequiredService<ILogger<AMQP10MessengerImpl>>());
                }

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                amqpMessenger.InitializeAsync().Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

                return amqpMessenger;
            });

            Console.WriteLine("AMQP initialized successfully");

            return services;
        }

        private static int[] GetListenerCounts(IConfiguration configuration, string[] addressesSplit)
        {
            var amqpListenerCounts = new int[addressesSplit.Length];

            for (int i = 0; i < addressesSplit.Length; i++)
            {
                int? amqpListenerCount = configuration.GetValue<int?>($"Amqp:AddressDetails:{addressesSplit[i]}:ListenerCount");

                if (amqpListenerCount == null)
                {
                    throw new Exception($"AMQP listener count for address {addressesSplit[i]} is not defined");
                }

                amqpListenerCounts[i] = (int)amqpListenerCount;
            }

            return amqpListenerCounts;
        }

        private static bool[] GetSessions(IConfiguration configuration, string[] addressesSplit, bool useServiceBus, bool useActiveMq)
        {
            var amqpIsSessions = new bool[addressesSplit.Length];

            for (int i = 0; i < addressesSplit.Length; i++)
            {
                bool? isSession = configuration.GetValue<bool?>($"Amqp:AddressDetails:{addressesSplit[i]}:IsSession");

                amqpIsSessions[i] = isSession ?? false;

                if (!useServiceBus && !useActiveMq && amqpIsSessions[i])
                {
                    throw new Exception($"AMQP 1.0 implementation does not support sessions");
                }
            }

            return amqpIsSessions;
        }
    }
}
