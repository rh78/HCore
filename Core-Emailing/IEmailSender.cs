using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Emailing
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string configurationKey, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage);
    }
}
