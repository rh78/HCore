using System;
using Segment;

namespace HCore.Segment.Providers.Impl
{
    internal class SegmentProviderImpl : ISegmentProvider
    {
        public string TrackingKey => Analytics.Client.WriteKey;

        public Client GetSegmentClient()
        {
            return Analytics.Client;
        }
    }
}
