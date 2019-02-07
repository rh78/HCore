using Segment;

namespace HCore.Segment.Providers
{
    public interface ISegmentProvider
    {
        string TrackingKey { get; }

        Client GetSegmentClient();
    }
}
