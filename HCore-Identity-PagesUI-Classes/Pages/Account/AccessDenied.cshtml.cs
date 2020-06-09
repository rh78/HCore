using HCore.Identity.Attributes;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class AccessDeniedModel : BasePageModelProvidingJsonModelData
    {
        public override string ModelAsJson { get; } = "{}";

        public void OnGet()
        {

        }
    }
}
