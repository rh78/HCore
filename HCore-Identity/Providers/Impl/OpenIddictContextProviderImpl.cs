using System;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch.MachineLearning;
using HCore.Identity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace HCore.Identity.Providers.Impl
{
    internal class OpenIddictContextProviderImpl : IOpenIddictContextProvider
    {
        public Task<OpenIddictContextModel> GetOpenIddictContextAsync(string returnUrl, IUrlHelper urlHelper)
        {
            if (!IsValidReturnUrl(returnUrl, urlHelper))
            {
                return Task.FromResult<OpenIddictContextModel>(null);
            }

            returnUrl = $"http://mock.com{returnUrl}";

            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
            {
                return Task.FromResult<OpenIddictContextModel>(null);
            }

            var query = QueryHelpers.ParseQuery(uri.Query);

            if (!query.TryGetValue("client_id", out var clientId) ||
                string.IsNullOrEmpty(clientId))
            {
                return Task.FromResult<OpenIddictContextModel>(null);
            }

            if (!query.TryGetValue("redirect_uri", out var redirectUri) ||
                string.IsNullOrEmpty(redirectUri))
            {
                return Task.FromResult<OpenIddictContextModel>(null);
            }

            return Task.FromResult(new OpenIddictContextModel()
            {
                ClientId = clientId,
                RedirectUri = redirectUri
            });
        }

        public bool IsValidReturnUrl(string returnUrl, IUrlHelper urlHelper)
        {
            if (!urlHelper.IsLocalUrl(returnUrl))
            {
                return false;
            }

            returnUrl = $"http://mock.com{returnUrl}";

            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
            {
                return false;
            }

            var path = uri.LocalPath;

            if (path.EndsWith("/connect/authorize", StringComparison.Ordinal) ||
                path.EndsWith("/connect/authorize/callback", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }
}
