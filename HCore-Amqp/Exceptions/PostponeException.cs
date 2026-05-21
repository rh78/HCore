using System;

namespace HCore.Amqp.Exceptions
{
    public class PostponeException : Exception
    {
        public TimeSpan? LockSessionTimeSpan { get; private set; }

        public PostponeException(TimeSpan? lockSessionTimeSpan = null)
            : base()
        {
            LockSessionTimeSpan = lockSessionTimeSpan;
        }
    }
}
