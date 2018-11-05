using System;
using System.Collections.Generic;
using System.Text;

namespace HCore.Web.Providers
{
    public interface ISpaManifestJsonProvider
    {
        string HeaderIncludes { get; }
        string BodyIncludes { get; }
    }
}
