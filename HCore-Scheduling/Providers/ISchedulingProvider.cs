using HCore.Scheduling.Models;
using Quartz;

namespace HCore.Scheduling.Providers
{
    public interface ISchedulingProvider
    {
        void StartJob(ISchedulingJob job, ITrigger jobTrigger);
    }
}
