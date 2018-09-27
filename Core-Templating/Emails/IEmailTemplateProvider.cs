using ReinhardHolzner.Core.Templating.Emails.ViewModels;
using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Templating.Emails
{
    public interface IEmailTemplateProvider
    {
        Task<EmailTemplate> GetConfirmAccountEmailAsync(ConfirmAccountEmailViewModel confirmAccountEmailViewModel);
    }
}
