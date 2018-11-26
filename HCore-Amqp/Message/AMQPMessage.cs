namespace HCore.Amqp.Message
{
    public class AMQPMessage
    {
        public string Action { get; private set; }
        
        public AMQPMessage(string action)
        {
            Action = action;
        }
    }
}
