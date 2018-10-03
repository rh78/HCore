﻿using System;

namespace ReinhardHolzner.Core.Web.Providers.Impl
{
    internal class NowProviderImpl : INowProvider
    {
        public DateTimeOffset Now { get; private set; }

        public NowProviderImpl()
        {
            Now = DateTimeOffset.Now;
        }
    }
}