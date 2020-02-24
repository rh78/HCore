using PusherServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCore.Pusher.Messenger
{
    public interface IPusherMessenger
    {
        string AuthenticateListener(string channelId, string socketId);

        Task SendMessageAsync(string channelName, string eventName, object data);
        Task SendMessagesAsync(List<Event> events);
    }
}
