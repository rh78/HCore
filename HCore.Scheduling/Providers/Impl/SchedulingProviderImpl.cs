using Microsoft.Extensions.Configuration;
using Quartz;
using System;
using System.Reflection;

namespace HCore.Scheduling.Providers.Impl
{
    internal class SchedulingProviderImpl : ISchedulingProvider
    {
        private readonly IScheduler _scheduler;

        public SchedulingProviderImpl(IConfiguration configuration, IScheduler scheduler)
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

                var jobInstance = JobBuilder.Create(jobType)
                    .WithIdentity(job)
                    .Build();

                var jobTrigger = TriggerBuilder.Create()
                    .WithIdentity(job)
                    .WithCronSchedule(cronScheduler)
                    .StartNow()
                    .Build();

                scheduler.ScheduleJob(jobInstance, jobTrigger);

                Console.WriteLine($"Job {job} scheduled successfully");
            }

            _scheduler = scheduler;
        }

        public void StartJob(IJob job, ITrigger jobTrigger)
        {
            var jobType = job.GetType();

            var jobName = jobType.Name;

            var jobInstance = JobBuilder.Create(jobType)
              .WithIdentity(jobName)
              .Build();

            _scheduler.ScheduleJob(jobInstance, jobTrigger);
        }
    }
}
