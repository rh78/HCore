using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using System;
using System.Threading.Tasks;

namespace HCore.Scheduling.Factories.Impl
{
    internal class SchedulingJobFactoryImpl : ISchedulingJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SchedulingJobFactoryImpl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var scope = _serviceProvider.CreateScope();

            var jobDetail = bundle.JobDetail;

            var job = (IJob) scope.ServiceProvider.GetService(jobDetail.JobType);

            return new ScopedJob(job, scope);
        }

        public void ReturnJob(IJob job)
        {
            
        }

        internal class ScopedJob : IJob
        {
            private IJob _job;
            private IServiceScope _scope;

            public ScopedJob(IJob job, IServiceScope scope)
            {
                _job = job;
                _scope = scope;
            }

            public async Task Execute(IJobExecutionContext context)
            {
                try
                {
                    await _job.Execute(context).ConfigureAwait(false);
                } catch (Exception e)
                {
                    _scope.Dispose();

                    throw e;
                }

                _scope.Dispose();
            }
        }
    }
}
