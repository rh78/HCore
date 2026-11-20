using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore;
using Xunit;

namespace HCore.Test
{
    // the "Sentry" section of the hosts, bound the way Launcher wires it with webHostBuilder.UseSentry(), so a renamed or removed option shows here
    public class SentryOptionsTests
    {
        private const string Dsn = "https://public@sentry.example.com/1";

        [Fact]
        public void SentrySection_BindsTheOptionsTheHostsSet()
        {
            var settings = new Dictionary<string, string>
            {
                { "Sentry:Dsn", Dsn },
                { "Sentry:SendDefaultPii", "true" },
                { "Sentry:MinimumBreadcrumbLevel", "Information" },
                { "Sentry:MinimumEventLevel", "Warning" },
                { "Sentry:AttachStackTrace", "true" }
            };

            using var host = new HostBuilder()
                .ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(settings))
                .ConfigureWebHost(webHostBuilder => webHostBuilder.UseSentry())
                .Build();

            var sentryOptions = host.Services.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;

            Assert.Equal(Dsn, sentryOptions.Dsn);
            Assert.True(sentryOptions.SendDefaultPii);
            Assert.Equal(LogLevel.Information, sentryOptions.MinimumBreadcrumbLevel);
            Assert.Equal(LogLevel.Warning, sentryOptions.MinimumEventLevel);
            Assert.True(sentryOptions.AttachStacktrace);
        }
    }
}
