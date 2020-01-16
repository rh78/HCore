using HCore.Scheduling.Factories.Impl;
using HCore.Scheduling.Providers;
using HCore.Scheduling.Providers.Impl;
using Microsoft.Extensions.Configuration;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Collections.Specialized;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SchedulingServiceCollectionExtensions
    {
        public static IServiceCollection AddScheduling(this IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Initializing scheduling...");

            services.AddSingleton<ISchedulingProvider, SchedulingProviderImpl>();

            services.AddSingleton<IJobFactory, SchedulingJobFactoryImpl>();
            
            services.AddSingleton(provider =>
            {
                var props = new NameValueCollection {
                    { "quartz.serializer.type", "json" }
                };

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                var schedulerFactory = new StdSchedulerFactory(props);
                var scheduler = schedulerFactory.GetScheduler().Result;
                scheduler.JobFactory = provider.GetService<IJobFactory>();

                scheduler.Clear().Wait();
                scheduler.Start().Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

                return scheduler;
            });

            string jobs = configuration["Scheduling:Jobs"];

            if (string.IsNullOrEmpty(jobs))
                throw new Exception("Scheduling jobs are missing");

            string[] jobsSplit = jobs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (jobsSplit.Length == 0)
                throw new Exception("Scheduling jobs are empty");

            var callingAssembly = Assembly.GetEntryAssembly();

            foreach (var job in jobsSplit)
            {
                var jobType = callingAssembly.GetType(job);

                if (jobType == null)
                    throw new Exception($"Job type for job {job} was not found");

                services.Add(new ServiceDescriptor(jobType, jobType, ServiceLifetime.Transient));
            }

            Console.WriteLine("Scheduling initialized successfully");

            return services;
        }        
    }
}
