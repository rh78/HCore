namespace ReinhardHolzner.Core.AMQP
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
