using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCore.Emailing
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string configurationKey, string fromOverride, List<string> to, List<string> cc, List<string> bcc, string subject, string htmlMessage);
    }
}
