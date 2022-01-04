using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCore.Amqp.Processor
{
    public interface IAMQPMessageProcessor
    {
        Task<bool> ProcessMessageAsync(string address, string messageBodyJson);
        
        Task<bool> ProcessMessagesAsync(string address, List<string> messageBodyJsons);
    }
}
