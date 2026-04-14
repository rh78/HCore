// solution from here: https://stackoverflow.com/questions/72204254/scopes-output-in-token
using System.Threading.Tasks;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace HCore.Identity.Internal
{
    public class ScopesAsArrayHandler : IOpenIddictServerHandler<OpenIddictServerEvents.GenerateTokenContext>
    {
        public ValueTask HandleAsync(OpenIddictServerEvents.GenerateTokenContext context)
        {
            // backup OpenIdDict's custom scope claims
            
            var scopes = context.Principal.GetClaims("oi_scp");

            // remove OpenIdDict's custom scope claims
            
            context.Principal.RemoveClaims("oi_scp");

            // add backed up scopes as "scope" claims

            context.Principal.RemoveClaims("scope");
            context.Principal.AddClaims("scope", scopes);

            return ValueTask.CompletedTask;
        }
    }
}
