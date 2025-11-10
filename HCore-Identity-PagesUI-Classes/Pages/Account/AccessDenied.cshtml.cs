using HCore.Identity.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class AccessDeniedModel : BasePageModelProvidingJsonModelData
    {
        public override string ModelAsJson { get; } = "{}";

        public void OnGet()
        {

        }
    }
}
