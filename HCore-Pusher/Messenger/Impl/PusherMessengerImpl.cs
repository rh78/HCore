using System;
using System.Threading.Tasks;
using PusherServer;

namespace HCore.Pusher.Messenger.Impl
{
    internal class PusherMessengerImpl : IPusherMessenger
    {
        private readonly PusherServer.Pusher _pusher;        

        public PusherMessengerImpl(string cluster, string appId, string appKey, string appSecret)
        {
            var options = new PusherOptions
            {
                Cluster = cluster,
                Encrypted = true
            };

            _pusher = new PusherServer.Pusher(appId, appKey, appSecret, options);
        }

        public async Task SendMessageAsync(string channelName, string eventName, object data)
        {
            var result = await _pusher.TriggerAsync(channelName, eventName, data).ConfigureAwait(false);

        }
    }
}
