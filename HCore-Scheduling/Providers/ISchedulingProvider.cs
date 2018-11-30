using Quartz;

namespace HCore.Scheduling.Providers
{
    public interface ISchedulingProvider
    {
        void StartJob(IJob job, ITrigger jobTrigger);
    }
}
