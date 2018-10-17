namespace HCore.Amqp
{
    public abstract class AMQPMessage
    {
        public string Action { get; private set; }
        
        public AMQPMessage(string action)
        {
            Action = action;
        }
    }
}
