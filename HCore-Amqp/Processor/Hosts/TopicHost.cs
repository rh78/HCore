using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using HCore.Amqp.Messenger.Impl;
using Microsoft.Extensions.Logging;

namespace HCore.Amqp.Processor.Hosts
{
    internal class TopicHost : ActiveMqHost
    {
        internal TopicHost(int listenersCount, string address, bool isSession, ActiveMqMessengerImpl activeMqMessengerImpl, IConnection connection, CancellationToken cancellationToken, ILogger<ActiveMqMessengerImpl> logger)
            : base(listenersCount, address, isSession, activeMqMessengerImpl, connection, cancellationToken, logger)
        {
        }

        protected override async Task<IDestination> GetDestinationAsync(ISession session, string address)
        {
            return await session.GetTopicAsync(address).ConfigureAwait(false);
        }
    }
}
