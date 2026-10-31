using System;

namespace HCore.Amqp.Sqs
{
    // the defaults mirror the Service Bus implementation; the tests shorten them

    internal class SqsTimings
    {
        // the visibility timeout every receive sets, the same as the Service Bus lock duration

        internal TimeSpan VisibilityTimeout { get; init; } = TimeSpan.FromMinutes(1);

        // while a message is processed, its visibility is extended to VisibilityTimeout at this interval

        internal TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(20);

        // SQS limit: a message cannot stay invisible longer than 12 hours after it was received

        internal TimeSpan MaximumVisibility { get; init; } = TimeSpan.FromHours(12);

        // a message that failed with an exception becomes visible again after this time, like an expiring Service Bus lock

        internal TimeSpan ErrorHold { get; init; } = TimeSpan.FromMinutes(1);

        // PostponeException without a time span: the Service Bus session queues hold 10 s, the other queues keep the 1 min lock

        internal TimeSpan FifoPostponeHold { get; init; } = TimeSpan.FromSeconds(10);
        internal TimeSpan StandardPostponeHold { get; init; } = TimeSpan.FromMinutes(1);

        internal int ReceiveWaitTimeSeconds { get; init; } = 20;

        internal TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);
    }
}
