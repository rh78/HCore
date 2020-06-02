using System;
using System.Collections.Generic;
using System.Text;

namespace HCore.Web.Providers
{
    public interface IHtmlIncludesProvider
    {
        bool Applies { get; }

        string HeaderIncludes { get; }
        string BodyIncludes { get; }

        string HeaderCssIncludes { get; }

        string HeaderJsIncludes { get; }
        string BodyJsIncludes { get; }
    }
}
