using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HCore.Identity.Validators.Impl
{
    public class WildcardRedirectUriValidatorImpl : IRedirectUriValidator
    {
        public Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
        {
            return MatchUriAsync(requestedUri, client.RedirectUris);
        }

        public Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
        {
            return MatchUriAsync(requestedUri, client.PostLogoutRedirectUris);
        }

        private const string WildcardCharacter = @"[a-zA-Z0-9\-]";

        private Task<bool> MatchUriAsync(string requestedUri, ICollection<string> allowedUris)
        {
            var rules = allowedUris.Select(ConvertToRegex).ToList();
            var res = rules.Any(r => Regex.IsMatch(requestedUri, r, RegexOptions.IgnoreCase));
            return Task.FromResult(res);
        }

        private static string ConvertToRegex(string rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            return Regex.Escape(rule)
                        .Replace(@"\*", WildcardCharacter + "*")
                        .Replace(@"\?", WildcardCharacter);
        }
    }
}
