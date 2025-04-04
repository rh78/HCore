﻿using Microsoft.Extensions.Configuration;
using HCore.Emailing.Sender;
using HCore.Emailing.Sender.Impl;
using System;
using System.Linq;
using HCore.Emailing;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EmailingServiceCollectionExtensions
    {
        public static IServiceCollection AddEmailing(this IServiceCollection services, IConfiguration configuration)
        {
            string addresses = configuration["Amqp:Addresses"];

            if (string.IsNullOrEmpty(addresses))
                throw new Exception("AMQP addresses are missing");

            string[] addressesSplit = addresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (!addressesSplit.Any(address => address.EndsWith(EmailSenderConstants.AddressSuffix)))
                throw new Exception($"AMQP email sender requires the AMQP address suffix {EmailSenderConstants.AddressSuffix}' to be defined");

            bool useAmqpEmailSender = configuration.GetValue<bool>("Emailing:UseAmqpEmailSender");

            if (useAmqpEmailSender)
            {
                Console.WriteLine("Initializing AMQP email sender...");

                services.AddAmqp(configuration);

                services.AddSingleton<IEmailSender, AMQPEmailSenderImpl>();

                services.AddSingleton<IDirectEmailSender, DirectEmailSenderImpl>();

                Console.WriteLine("AMQP email sender initialized successfully");
            }
            else
            {
                Console.WriteLine("Initializing direct email sender...");

                services.AddSingleton<IEmailSender, DirectEmailSenderImpl>();

                services.AddSingleton<IDirectEmailSender, DirectEmailSenderImpl>();

                Console.WriteLine("Direct email sender initialized successfully");
            }

            return services;
        }        
    }
}
