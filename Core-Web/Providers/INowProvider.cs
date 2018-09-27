using System;

namespace ReinhardHolzner.Core.Web.Providers
{
    public interface INowProvider
    {
        DateTimeOffset Now { get; }
    }
}
