using System.Runtime.Serialization;

namespace HCore.Templating.Configuration
{
    public class HackedChromeConfiguration : jsreport.Types.ChromeConfiguration
    {
        public ChromeLaunchOptionsConfiguration LaunchOptions { get; set; }

        public class ChromeLaunchOptionsConfiguration
        {
            [DataMember(Name = "chrome_launchOptions_args")]
            public string Args { get; set; }
        }
    }
}
