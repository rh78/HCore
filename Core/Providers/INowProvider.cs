using System;

namespace ReinhardHolzner.Core.Providers
{
    public interface INowProvider
    {
        DateTimeOffset Now { get; }
    }
}
