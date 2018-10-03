﻿using Microsoft.Extensions.DependencyInjection;
using ReinhardHolzner.Core.Amqp.Processor;

namespace Microsoft.AspNetCore.Builder
{
    public static class AmqpApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAmqp(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetRequiredService<IAMQPMessenger>();

            return app;
        }
    }
}