using HCore.Tenants.Providers;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HCore.Identity.EmailSender.Impl
{
    public class EmailSenderImpl : IEmailSender
    {
        private readonly Emailing.Sender.IEmailSender _emailSender;

        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        public EmailSenderImpl(Emailing.Sender.IEmailSender emailSender, IServiceProvider serviceProvider)
        {
            _emailSender = emailSender;

            _tenantInfoAccessor = serviceProvider.GetService<ITenantInfoAccessor>();
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            string fromOverride = _tenantInfoAccessor != null ? _tenantInfoAccessor.TenantInfo.NoreplyEmail : null;
            string fromDisplayNameOverride = _tenantInfoAccessor != null ? _tenantInfoAccessor.TenantInfo.NoreplyEmailDisplayName : null;

            await _emailSender.SendEmailAsync(null, fromOverride, fromDisplayNameOverride, new string[] { email }.ToList(), null, null, subject, htmlMessage).ConfigureAwait(false);
        }
    }
}
