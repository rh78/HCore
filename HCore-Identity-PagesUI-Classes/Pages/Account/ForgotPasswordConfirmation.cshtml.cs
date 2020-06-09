using HCore.Identity.Attributes;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    [SecurityHeaders]
    public class ForgotPasswordConfirmationModel : BasePageModelProvidingJsonModelData
    {
        public override string ModelAsJson { get; } = "{}";

        public void OnGet()
        {
        }
    }
}
