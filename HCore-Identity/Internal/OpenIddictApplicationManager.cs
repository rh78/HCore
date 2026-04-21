using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;

namespace HCore.Identity.Internal
{
    public class OpenIddictApplicationManager<TApplication> : OpenIddict.Core.OpenIddictApplicationManager<TApplication> where TApplication : OpenIddictEntityFrameworkCoreApplication
    {
        public OpenIddictApplicationManager(IOpenIddictApplicationCache<TApplication> cache, ILogger<OpenIddictApplicationManager<TApplication>> logger, IOptionsMonitor<OpenIddictCoreOptions> options, IOpenIddictApplicationStore<TApplication> store)
            : base(cache, logger, options, store)
        {
        }

        protected override ValueTask<string> ObfuscateClientSecretAsync(string secret, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(secret);

            if (!secret.StartsWith("|"))
            {
                return base.ObfuscateClientSecretAsync(secret, cancellationToken);
            }

            // make a second check - is this really a valid Base64 value

            try
            {
                var base64Secret = secret[1..];

                Convert.FromBase64String(base64Secret);
            }
            catch (Exception)
            {
                // its not Base64, so it is not a legacy secret

                return base.ObfuscateClientSecretAsync(secret, cancellationToken);
            }

            // this is a legacy secret coming from Identity Server

            return new(secret);
        }

        protected override ValueTask<bool> ValidateClientSecretAsync(
            string secret, string comparand, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(secret);
            ArgumentException.ThrowIfNullOrEmpty(comparand);

            if (!comparand.StartsWith("|"))
            {
                return base.ValidateClientSecretAsync(secret, comparand, cancellationToken);
            }

            // this is a legacy comparand

            comparand = comparand[1..];

            string base64hash;

            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(secret);
                var hash = sha.ComputeHash(bytes);

                base64hash = Convert.ToBase64String(hash);
            }

            return string.Equals(base64hash, comparand, StringComparison.Ordinal) ? new(true) : new(false);
        }

        public override async ValueTask<bool> ValidateRedirectUriAsync(TApplication application,
            [StringSyntax(StringSyntaxAttribute.Uri)] string uri, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentException.ThrowIfNullOrEmpty(uri);

            var candidates = await Store.GetRedirectUrisAsync(application, cancellationToken).ConfigureAwait(false);

            if (IsUriMatch(uri, candidates))
            {
                return true;
            }

            return false;
        }

        public override async ValueTask<bool> ValidatePostLogoutRedirectUriAsync(TApplication application,
            [StringSyntax(StringSyntaxAttribute.Uri)] string uri, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentException.ThrowIfNullOrEmpty(uri);

            var candidates = await Store.GetPostLogoutRedirectUrisAsync(application, cancellationToken).ConfigureAwait(false);

            if (IsUriMatch(uri, candidates))
            {
                return true;
            }

            return false;
        }

        private const string WildcardCharacter = @"[a-zA-Z0-9\-]";

        private bool IsUriMatch(string requestedUri, ICollection<string> allowedUris)
        {
            var rules = allowedUris.Select(ConvertToRegex).ToList();

            var matchingRuleFound = rules.Any(r => Regex.IsMatch(requestedUri, r, RegexOptions.IgnoreCase));
            return matchingRuleFound;
        }

        private static string ConvertToRegex(string rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            return Regex.Escape(rule)
                        .Replace(@"WILDCARD", WildcardCharacter + "*");
        }
    }
}
