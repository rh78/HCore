using System.Threading.Tasks;

namespace HCore.Pusher.Messenger
{
    public interface IPusherMessenger
    {
        Task SendMessageAsync(string channelName, string eventName, object data);
    }
}
