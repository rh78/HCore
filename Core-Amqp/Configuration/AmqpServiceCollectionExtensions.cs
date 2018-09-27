using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ReinhardHolzner.Core.Amqp;
using ReinhardHolzner.Core.Amqp.Processor;
using ReinhardHolzner.Core.Amqp.Processor.Impl;
using System;

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

            string connectionString = configuration["Amqp:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("AMQP connection string is empty");

            string addresses = configuration["Amqp:Addresses"];

            if (string.IsNullOrEmpty(addresses))
                throw new Exception("AMQP addresses are missing");

            string[] addressesSplit = addresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (addressesSplit.Length == 0)
                throw new Exception("AMQP addresses are empty");

            int[] amqpListenerCounts = new int[addressesSplit.Length];

            for (int i = 0; i < addressesSplit.Length; i++)
            {
                int amqpListenerCount = configuration.GetValue<int>($"Amqp:{addressesSplit[i]}ListenerCount");
                if (amqpListenerCount <= 0)
                    amqpListenerCount = 1;

                amqpListenerCounts[i] = amqpListenerCount;
            }

            services.AddSingleton(factory =>
            {
                IAMQPMessageProcessor messageProcessor = factory.GetRequiredService<IAMQPMessageProcessor>();

                IAMQPMessenger amqpMessenger;

                if (useServiceBus)
                {
                    // Service Bus
                    amqpMessenger = new ServiceBusMessengerImpl(connectionString, addressesSplit, amqpListenerCounts, factory.GetRequiredService<IApplicationLifetime>(), messageProcessor);
                }
                else
                {
                    // AMQP 1.0

                    amqpMessenger = new AMQP10MessengerImpl(connectionString, addressesSplit, amqpListenerCounts, factory.GetRequiredService<IApplicationLifetime>(), messageProcessor);
                }

                amqpMessenger.InitializeAsync().Wait();

                return amqpMessenger;
            });

            Console.WriteLine("AMQP initialized successfully");

            return services;
        }
    }
}
