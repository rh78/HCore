using Microsoft.AspNetCore.Identity.UI.Services;
using System.Linq;
using System.Threading.Tasks;

namespace HCore.Identity.EmailSender.Impl
{
    public class EmailSenderImpl : IEmailSender
    {
        private readonly Emailing.IEmailSender _emailSender;

        public EmailSenderImpl(Emailing.IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await _emailSender.SendEmailAsync(null, new string[] { email }.ToList(), null, null, subject, htmlMessage).ConfigureAwait(false);
        }
    }
}
