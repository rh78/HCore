using Microsoft.Extensions.Primitives;

namespace HCore.Identity.Models
{
    public class OpenIddictContextModel
    {
        public StringValues ClientId { get; internal set; }
        public StringValues RedirectUri { get; internal set; }
    }
}
