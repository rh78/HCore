using System.Threading;
using HCore.Amqp.Messenger.Impl;
using Microsoft.Extensions.Logging;

namespace HCore.Amqp.Processor.Hosts
{
    internal class QueueHost : ActiveMqHost
    {
        internal QueueHost(int listenersCount, string address, bool isSession, ActiveMqMessengerImpl activeMqMessengerImpl, CancellationToken cancellationToken, ILogger<ActiveMqMessengerImpl> logger)
            : base(listenersCount, address, isSession, activeMqMessengerImpl, cancellationToken, logger)
        {
        }
    }
}
