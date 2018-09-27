using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(ReinhardHolzner.Core.Identity.PagesUI.Classes.Areas.Identity.IdentityHostingStartup))]
namespace ReinhardHolzner.Core.Identity.PagesUI.Classes.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}