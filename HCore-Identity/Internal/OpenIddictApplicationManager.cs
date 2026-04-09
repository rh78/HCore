using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Elastic.Clients.Elasticsearch.MachineLearning;
using FluentAssertions.Equivalency;
using HCore.Identity.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;

namespace HCore.Identity.Internal
{
    internal class OpenIddictApplicationManager<TApplication> : OpenIddict.Core.OpenIddictApplicationManager<TApplication> where TApplication : OpenIddictEntityFrameworkCoreApplication
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
    }
}
