using HCore.Templating.Emails.ViewModels;
using System.Globalization;
using System.Threading.Tasks;

namespace HCore.Templating.Emails
{
    public interface IEmailTemplateProvider
    {
        Task<EmailTemplate> GetConfirmAccountEmailAsync(ConfirmAccountEmailViewModel confirmAccountEmailViewModel, CultureInfo cultureInfo);
        Task<EmailTemplate> GetForgotPasswordEmailAsync(ForgotPasswordEmailViewModel forgotPasswordEmailViewModel, CultureInfo cultureInfo);
    }
}
