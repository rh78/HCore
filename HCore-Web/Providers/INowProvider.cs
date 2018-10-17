using System;

namespace HCore.Web.Providers
{
    public interface INowProvider
    {
        DateTimeOffset Now { get; }
    }
}
