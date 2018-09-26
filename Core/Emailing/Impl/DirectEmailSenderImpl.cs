using System;
using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Emailing.Impl
{
    internal class DirectEmailSenderImpl : EmailSenderImpl, IEmailSender
    {
#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            throw new NotImplementedException();
        }
    }
}
