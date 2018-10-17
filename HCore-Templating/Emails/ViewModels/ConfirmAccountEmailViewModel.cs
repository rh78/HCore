using HCore.Templating.Emails.ViewModels.Shared;

namespace HCore.Templating.Emails.ViewModels
{
    public class ConfirmAccountEmailViewModel : EmailViewModel
    {
        public string ConfirmEmailUrl { get; set; }

        public ConfirmAccountEmailViewModel(string confirmEmailUrl)
            : base()
        {
            ConfirmEmailUrl = confirmEmailUrl;
        }        
    }
}
