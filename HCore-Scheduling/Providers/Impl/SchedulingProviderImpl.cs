using System;
using System.Reflection;
using HCore.Scheduling.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using static System.Formats.Asn1.AsnWriter;

namespace HCore.Scheduling.Providers.Impl
{
    internal class SchedulingProviderImpl : ISchedulingProvider
    {
        private readonly IScheduler _scheduler;

        public SchedulingProviderImpl(IConfiguration configuration, IScheduler scheduler, IServiceProvider serviceProvider)
        {
            string jobs = configuration["Scheduling:Jobs"];

            if (string.IsNullOrEmpty(jobs))
                throw new Exception("Scheduling jobs are missing");

            string[] jobsSplit = jobs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (jobsSplit.Length == 0)
                throw new Exception("Scheduling jobs are empty");

            var assembly = Assembly.GetEntryAssembly();

            foreach (var job in jobsSplit)
            {
                string cronScheduler = configuration[$"Scheduling:JobDetails:{job}:CronScheduler"];

                if (string.IsNullOrEmpty(cronScheduler))
                    throw new Exception($"CRON scheduler for job {job} is not defined");

                Console.WriteLine($"Scheduling job {job} with CRON scheduler {cronScheduler}...");

                var jobType = assembly.GetType(job);

                var jobDetail = JobBuilder.Create(jobType)
                    .WithIdentity(job)
                    .Build();

                var scope = serviceProvider.CreateScope();

                try
                { 
                    var jobInstance = (ISchedulingJob)scope.ServiceProvider.GetService(jobDetail.JobType);

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                    jobInstance.InitializeAsync().Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                }
                catch (Exception)
                {
                    scope.Dispose();

                    throw;
                }

                scope.Dispose();

                var jobTrigger = TriggerBuilder.Create()
                    .WithIdentity(job)
                    .WithCronSchedule(cronScheduler)
                    .StartNow()
                    .Build();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                scheduler.ScheduleJob(jobDetail, jobTrigger).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

                Console.WriteLine($"Job {job} scheduled successfully");
            }

            _scheduler = scheduler;
        }

        public void StartJob(ISchedulingJob job, ITrigger jobTrigger)
        {
            var jobType = job.GetType();

            var jobName = jobType.Name;

            var jobInstance = JobBuilder.Create(jobType)
              .WithIdentity(jobName)
              .Build();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            _scheduler.ScheduleJob(jobInstance, jobTrigger).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }
    }
}
