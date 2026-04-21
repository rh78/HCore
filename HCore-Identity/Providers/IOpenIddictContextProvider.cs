using System.Threading.Tasks;
using HCore.Identity.Models;
using Microsoft.AspNetCore.Mvc;

namespace HCore.Identity.Providers
{
    public interface IOpenIddictContextProvider
    {
        bool IsValidReturnUrl(string returnUrl, IUrlHelper urlHelper);

        Task<OpenIddictContextModel> GetOpenIddictContextAsync(string returnUrl, IUrlHelper urlHelper);
    }
}
