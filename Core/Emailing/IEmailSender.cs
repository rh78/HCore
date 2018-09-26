using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Emailing
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
