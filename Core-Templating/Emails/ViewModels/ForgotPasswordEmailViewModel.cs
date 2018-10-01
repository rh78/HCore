using ReinhardHolzner.Core.Templating.Emails.ViewModels.Shared;

namespace ReinhardHolzner.Core.Templating.Emails.ViewModels
{
    public class ForgotPasswordEmailViewModel : EmailViewModel
    {
        public string PasswordResetUrl { get; set; }

        public ForgotPasswordEmailViewModel(string passwordResetUrl)
            : base()
        {
            PasswordResetUrl = passwordResetUrl;
        }        
    }
}
