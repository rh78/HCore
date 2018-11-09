using HCore.Templating.Templates.ViewModels.Shared;

namespace HCore.Templating.Emails.ViewModels.Shared
{
    public class EmailViewModel : TemplateViewModel
    {
        public EmailViewModel()
        {            
        }
        
        public string Title { get; set; }
        public string PreHeader { get; set; }
    }
}
