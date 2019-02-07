using Segment;

namespace HCore.Segment.Providers
{
    public interface ISegmentProvider
    {
        Client GetSegmentClient();
    }
}
