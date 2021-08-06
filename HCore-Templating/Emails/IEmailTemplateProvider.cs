using HCore.Templating.Emails.ViewModels;
using System.Globalization;
using System.Threading.Tasks;

namespace HCore.Templating.Emails
{
    public interface IEmailTemplateProvider
    {
        Task<EmailTemplate> GetConfirmAccountEmailAsync(ConfirmAccountEmailViewModel confirmAccountEmailViewModel, bool? isPortals, CultureInfo cultureInfo);
        Task<EmailTemplate> GetForgotPasswordEmailAsync(ForgotPasswordEmailViewModel forgotPasswordEmailViewModel, bool? isPortals, CultureInfo cultureInfo);
    }
}
