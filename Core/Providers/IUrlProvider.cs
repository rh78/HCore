﻿namespace ReinhardHolzner.HCore.Providers
{
    public interface IUrlProvider
    {
        string ApiDomain { get; }
        string WebDomain { get; }

        string BuildApiUrl(string path);
        string BuildWebUrl(string path);
    }
}