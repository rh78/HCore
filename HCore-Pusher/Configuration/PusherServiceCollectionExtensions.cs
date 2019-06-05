using HCore.Pusher.Messenger;
using HCore.Pusher.Messenger.Impl;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EmailingServiceCollectionExtensions
    {
        public static IServiceCollection AddPusher(this IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Initializing Pusher...");

            string cluster = configuration["Pusher:Cluster"];

            if (string.IsNullOrEmpty(cluster))
                throw new Exception("Pusher cluster is empty");

            string appId = configuration["Pusher:AppId"];

            if (string.IsNullOrEmpty(appId))
                throw new Exception("Pusher app ID is empty");

            string appKey = configuration["Pusher:AppKey"];

            if (string.IsNullOrEmpty(appKey))
                throw new Exception("Pusher app key is empty");

            string appSecret = configuration["Pusher:AppSecret"];

            if (string.IsNullOrEmpty(appSecret))
                throw new Exception("Pusher app secret is empty");

            services.AddSingleton(factory =>
            {
                IPusherMessenger pusherMessenger = new PusherMessengerImpl(cluster, appId, appKey, appSecret);

                return pusherMessenger;
            });

            Console.WriteLine("Pusher initialized successfully");

            return services;
        }
    }
}
