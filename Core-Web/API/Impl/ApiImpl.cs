using System.Globalization;
using System.Text.RegularExpressions;

namespace ReinhardHolzner.Core.Web.API.Impl
{
    public class ApiImpl
    {
        public static readonly Regex Uuid = new Regex(@"^[a-zA-Z0-9_.-]+$");
        public static readonly Regex SafeString = new Regex(@"^[\w\s\.@_-]+$");

        public static readonly CultureInfo DefaultCultureInfo = CultureInfo.GetCultureInfo("en-US");
    }
}
