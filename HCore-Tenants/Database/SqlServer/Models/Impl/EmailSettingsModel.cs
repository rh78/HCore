using System;
using System.Collections.Generic;
using System.Globalization;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    [Serializable]
    public class EmailSettingsModel
    {
        public EmailInstanceSettingsModel InvitationEmailSettings { get; set; }
        public EmailInstanceSettingsModel ConfirmAccountEmailSettings { get; set; }
        public EmailInstanceSettingsModel ForgotPasswordEmailSettings { get; set; }
        public EmailInstanceSettingsModel NewUnreadNotificationsEmailSettings { get; set; }

        public void MergeWith(CustomEmailSettingsModel customEmailSettingsModel)
        {
            if (customEmailSettingsModel == null)
                return;

            if (customEmailSettingsModel.InvitationEmailSettings != null)
            {
                if (InvitationEmailSettings == null)
                    InvitationEmailSettings = customEmailSettingsModel.InvitationEmailSettings;
                else
                    InvitationEmailSettings.MergeWith(customEmailSettingsModel.InvitationEmailSettings);
            }

            if (customEmailSettingsModel.ConfirmAccountEmailSettings != null)
            {
                if (ConfirmAccountEmailSettings == null)
                    ConfirmAccountEmailSettings = customEmailSettingsModel.ConfirmAccountEmailSettings;
                else
                    ConfirmAccountEmailSettings.MergeWith(customEmailSettingsModel.ConfirmAccountEmailSettings);
            }

            if (customEmailSettingsModel.ForgotPasswordEmailSettings != null)
            {
                if (ForgotPasswordEmailSettings == null)
                    ForgotPasswordEmailSettings = customEmailSettingsModel.ForgotPasswordEmailSettings;
                else
                    ForgotPasswordEmailSettings.MergeWith(customEmailSettingsModel.ForgotPasswordEmailSettings);
            }

            if (customEmailSettingsModel.NewUnreadNotificationsEmailSettings != null)
            {
                if (NewUnreadNotificationsEmailSettings == null)
                    NewUnreadNotificationsEmailSettings = customEmailSettingsModel.NewUnreadNotificationsEmailSettings;
                else
                    NewUnreadNotificationsEmailSettings.MergeWith(customEmailSettingsModel.NewUnreadNotificationsEmailSettings);
            }
        }

        public void Validate()
        {
            if (InvitationEmailSettings == null)
                throw new Exception("Invitation email settings are missing");

            InvitationEmailSettings.Validate();

            if (ConfirmAccountEmailSettings == null)
                throw new Exception("Confirm account email settings are missing");

            ConfirmAccountEmailSettings.Validate();

            if (ForgotPasswordEmailSettings == null)
                throw new Exception("Forgot password email settings are missing");

            ForgotPasswordEmailSettings.Validate();

            if (NewUnreadNotificationsEmailSettings == null)
                throw new Exception("New unread notifications email settings are missing");

            NewUnreadNotificationsEmailSettings.Validate();
        }
    }

    [Serializable]
    public class EmailInstanceSettingsModel
    {
        public Dictionary<string, string> Subject { get; set; }
        public Dictionary<string, string> Title { get; set; }
        public Dictionary<string, string> PreHeader { get; set; }
        public Dictionary<string, string> TextPrefix { get; set; }
        public Dictionary<string, string> Button { get; set; }
        public Dictionary<string, string> TextSuffix { get; set; }
        public Dictionary<string, string> Footer { get; set; }

        public string GetSubject(CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }

        public string GetTitle(CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }

        public string GetPreHeader(CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }

        public string GetTextPrefix(CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }

        public string GetButton(CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }

        public string GetTextSuffix(CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }

        public string GetFooter(CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }

        public void Validate()
        {
            if (Subject == null || Subject.Count == 0)
                throw new Exception("Subject is missing");

            if (Title == null || Title.Count == 0)
                throw new Exception("Title is missing");

            if (PreHeader == null || PreHeader.Count == 0)
                throw new Exception("Preheader is missing");

            if (TextPrefix == null || TextPrefix.Count == 0)
                throw new Exception("Text prefix is missing");

            if (Button == null || Button.Count == 0)
                throw new Exception("Button is missing");

            if (TextSuffix == null || TextSuffix.Count == 0)
                throw new Exception("Text suffix is missing");

            if (Footer == null || Footer.Count == 0)
                throw new Exception("Footer is missing");
        }

        public void MergeWith(EmailInstanceSettingsModel emailInstanceSettingsModel)
        {
            if (emailInstanceSettingsModel.Subject != null &&
                emailInstanceSettingsModel.Subject.Count > 0)
            {
                Subject = emailInstanceSettingsModel.Subject;
            }

            if (emailInstanceSettingsModel.Title != null &&
                emailInstanceSettingsModel.Title.Count > 0)
            {
                Title = emailInstanceSettingsModel.Title;
            }

            if (emailInstanceSettingsModel.PreHeader != null &&
                emailInstanceSettingsModel.PreHeader.Count > 0)
            {
                PreHeader = emailInstanceSettingsModel.PreHeader;
            }

            if (emailInstanceSettingsModel.TextPrefix != null &&
                emailInstanceSettingsModel.TextPrefix.Count > 0)
            {
                TextPrefix = emailInstanceSettingsModel.TextPrefix;
            }

            if (emailInstanceSettingsModel.TextSuffix != null &&
                emailInstanceSettingsModel.TextSuffix.Count > 0)
            {
                TextSuffix = emailInstanceSettingsModel.TextSuffix;
            }

            if (emailInstanceSettingsModel.Button != null &&
                emailInstanceSettingsModel.Button.Count > 0)
            {
                Button = emailInstanceSettingsModel.Button;
            }

            if (emailInstanceSettingsModel.Footer != null &&
                emailInstanceSettingsModel.Footer.Count > 0)
            {
                Footer = emailInstanceSettingsModel.Footer;
            }
        }
    }
}
