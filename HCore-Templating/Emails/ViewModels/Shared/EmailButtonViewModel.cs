namespace HCore.Templating.Emails.ViewModels.Shared
{
    public class EmailButtonViewModel
    {
        public EmailButtonViewModel(string text, string url, string backgroundColor, string textColor)
        {
            Text = text;
            Url = url;

            BackgroundColor = backgroundColor;
            TextColor = textColor;
        }

        public string Text { get; set; }
        public string Url { get; set; }

        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
    }
}
