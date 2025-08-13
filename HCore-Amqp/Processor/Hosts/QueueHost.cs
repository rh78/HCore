using System.Threading;
using Apache.NMS;
using HCore.Amqp.Messenger.Impl;
using Microsoft.Extensions.Logging;

namespace HCore.Amqp.Processor.Hosts
{
    internal class QueueHost : ActiveMqHost
    {
        internal QueueHost(int listenersCount, string address, bool isSession, ActiveMqMessengerImpl activeMqMessengerImpl, IConnection connection, CancellationToken cancellationToken, ILogger<ActiveMqMessengerImpl> logger)
            : base(listenersCount, address, isSession, activeMqMessengerImpl, connection, cancellationToken, logger)
        {
        }
    }
}
