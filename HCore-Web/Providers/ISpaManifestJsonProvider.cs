using System;
using System.Collections.Generic;
using System.Text;

namespace HCore.Web.Providers
{
    public interface ISpaManifestJsonProvider
    {
        bool Applies { get; }

        string HeaderIncludes { get; }
        string BodyIncludes { get; }
    }
}
