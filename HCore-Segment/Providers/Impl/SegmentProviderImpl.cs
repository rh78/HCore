using Segment;

namespace HCore.Segment.Providers.Impl
{
    internal class SegmentProviderImpl : ISegmentProvider
    {
        public Client GetSegmentClient()
        {
            return Analytics.Client;
        }
    }
}
