using System.Threading.Tasks;

namespace HCore.Scheduling.Models
{
    public interface ISchedulingJob : Quartz.IJob
    {
        Task InitializeAsync();
    }
}
