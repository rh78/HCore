using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP.Internal
{
    internal interface IReceiverLinkHostMessageProcessor
    {
        Task ProcessMessageAsync(string address, object body);
    }
}
