using System;

namespace ReinhardHolzner.Core.Providers.Impl
{
    public class NowProviderImpl : INowProvider
    {
        public DateTimeOffset Now { get; private set; }

        public NowProviderImpl()
        {
            Now = DateTimeOffset.Now;
        }
    }
}
