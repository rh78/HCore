using ReinhardHolzner.Core.Templating.Emails.ViewModels.Shared;

namespace ReinhardHolzner.Core.Templating.Emails.ViewModels
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
