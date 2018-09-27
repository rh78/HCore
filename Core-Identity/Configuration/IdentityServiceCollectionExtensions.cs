using Microsoft.AspNetCore.Identity.UI.Services;
using ReinhardHolzner.Core.Identity.Impl;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IServiceCollection AddIdentity(this IServiceCollection services)
        {
            services.AddSingleton<IEmailSender, EmailSenderImpl>();

            return services;
        }        
    }
}
