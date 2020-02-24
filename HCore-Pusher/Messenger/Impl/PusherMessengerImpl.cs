using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PusherServer;

namespace HCore.Pusher.Messenger.Impl
{
    internal class PusherMessengerImpl : IPusherMessenger
    {
        private const int MaxPusherBatchSize = 10;

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

        public string AuthenticateListener(string channelId, string socketId)
        {
            var authenticationData = _pusher.Authenticate(channelId, socketId);

            return authenticationData.auth;
        }

        public async Task SendMessageAsync(string channelName, string eventName, object data)
        {
            await _pusher.TriggerAsync(channelName, eventName, data).ConfigureAwait(false);            
        }

        public async Task SendMessagesAsync(List<Event> events)
        {
            // split to trigger batches

            List<Event> currentBatch = new List<Event>();
            int currentBatchSize = 0;

            foreach (var _event in events) 
            {
                currentBatch.Add(_event);
                currentBatchSize++;

                if (currentBatchSize >= MaxPusherBatchSize)
                {
                    await _pusher.TriggerAsync(currentBatch.ToArray()).ConfigureAwait(false);

                    currentBatch.Clear();
                    currentBatchSize = 0;
                }
            }

            if (currentBatchSize > 0)
            {
                await _pusher.TriggerAsync(currentBatch.ToArray()).ConfigureAwait(false);
            }
        }
    }
}
