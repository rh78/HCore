using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    [Serializable]
    public class EmailSettingsModel
    {
        public EmailInstanceSettingsModel InvitationEmailSettings { get; set; }
        public EmailInstanceSettingsModel ConfirmAccountEmailSettings { get; set; }
        public EmailInstanceSettingsModel ForgotPasswordEmailSettings { get; set; }
        public EmailInstanceSettingsModel NewUnreadNotificationsEmailSettings { get; set; }
        public EmailInstanceSettingsModel CollectionInvitationEmailSettings { get; set; }
        public EmailInstanceSettingsModel CollectionInvitationAcceptedEmailSettings { get; set; }
        public EmailInstanceSettingsModel CollectionInvitationDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel DownloadAvailableEmailSettings { get; set; }
        public EmailInstanceSettingsModel DownloadFailedEmailSettings { get; set; }
        public EmailInstanceSettingsModel DownloadDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel AccessRequestDoneEmailSettings { get; set; }
        public EmailInstanceSettingsModel AccessRequestDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel PermissionRequestDoneEmailSettings { get; set; }
        public EmailInstanceSettingsModel PermissionRequestDeclinedEmailSettings { get; set; }

        public SmtpEmailSenderConfigurationModel EmailSenderConfiguration { get; set; }

        public void MergeWith(EmailSettingsModel customEmailSettingsModel)
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

            if (customEmailSettingsModel.CollectionInvitationEmailSettings != null)
            {
                if (CollectionInvitationEmailSettings == null)
                    CollectionInvitationEmailSettings = customEmailSettingsModel.CollectionInvitationEmailSettings;
                else
                    CollectionInvitationEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationEmailSettings);
            }

            if (customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings != null)
            {
                if (CollectionInvitationAcceptedEmailSettings == null)
                    CollectionInvitationAcceptedEmailSettings = customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings;
                else
                    CollectionInvitationAcceptedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings);
            }

            if (customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings != null)
            {
                if (CollectionInvitationDeclinedEmailSettings == null)
                    CollectionInvitationDeclinedEmailSettings = customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings;
                else
                    CollectionInvitationDeclinedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings);
            }

            if (customEmailSettingsModel.DownloadAvailableEmailSettings != null)
            {
                if (DownloadAvailableEmailSettings == null)
                    DownloadAvailableEmailSettings = customEmailSettingsModel.DownloadAvailableEmailSettings;
                else
                    DownloadAvailableEmailSettings.MergeWith(customEmailSettingsModel.DownloadAvailableEmailSettings);
            }

            if (customEmailSettingsModel.DownloadFailedEmailSettings != null)
            {
                if (DownloadFailedEmailSettings == null)
                    DownloadFailedEmailSettings = customEmailSettingsModel.DownloadFailedEmailSettings;
                else
                    DownloadFailedEmailSettings.MergeWith(customEmailSettingsModel.DownloadFailedEmailSettings);
            }
            
            if (customEmailSettingsModel.DownloadDeclinedEmailSettings != null)
            {
                if (DownloadDeclinedEmailSettings == null)
                    DownloadDeclinedEmailSettings = customEmailSettingsModel.DownloadDeclinedEmailSettings;
                else
                    DownloadDeclinedEmailSettings.MergeWith(customEmailSettingsModel.DownloadDeclinedEmailSettings);
            }

            if (customEmailSettingsModel.AccessRequestDoneEmailSettings != null)
            {
                if (AccessRequestDoneEmailSettings == null)
                    AccessRequestDoneEmailSettings = customEmailSettingsModel.AccessRequestDoneEmailSettings;
                else
                    AccessRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDoneEmailSettings);
            }

            if (customEmailSettingsModel.AccessRequestDeclinedEmailSettings != null)
            {
                if (AccessRequestDeclinedEmailSettings == null)
                    AccessRequestDeclinedEmailSettings = customEmailSettingsModel.AccessRequestDeclinedEmailSettings;
                else
                    AccessRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDeclinedEmailSettings);
            }

            if (customEmailSettingsModel.PermissionRequestDoneEmailSettings != null)
            {
                if (PermissionRequestDoneEmailSettings == null)
                    PermissionRequestDoneEmailSettings = customEmailSettingsModel.PermissionRequestDoneEmailSettings;
                else
                    PermissionRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDoneEmailSettings);
            }

            if (customEmailSettingsModel.PermissionRequestDeclinedEmailSettings != null)
            {
                if (PermissionRequestDeclinedEmailSettings == null)
                    PermissionRequestDeclinedEmailSettings = customEmailSettingsModel.PermissionRequestDeclinedEmailSettings;
                else
                    PermissionRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDeclinedEmailSettings);
            }

            if (customEmailSettingsModel.EmailSenderConfiguration != null)
            {
                EmailSenderConfiguration = customEmailSettingsModel.EmailSenderConfiguration;
            }
        }

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

            if (customEmailSettingsModel.CollectionInvitationEmailSettings != null)
            {
                if (CollectionInvitationEmailSettings == null)
                    CollectionInvitationEmailSettings = customEmailSettingsModel.CollectionInvitationEmailSettings;
                else
                    CollectionInvitationEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationEmailSettings);
            }

            if (customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings != null)
            {
                if (CollectionInvitationAcceptedEmailSettings == null)
                    CollectionInvitationAcceptedEmailSettings = customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings;
                else
                    CollectionInvitationAcceptedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings);
            }

            if (customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings != null)
            {
                if (CollectionInvitationDeclinedEmailSettings == null)
                    CollectionInvitationDeclinedEmailSettings = customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings;
                else
                    CollectionInvitationDeclinedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings);
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

            if (customEmailSettingsModel.DownloadAvailableEmailSettings != null)
            {
                if (DownloadAvailableEmailSettings == null)
                    DownloadAvailableEmailSettings = customEmailSettingsModel.DownloadAvailableEmailSettings;
                else
                    DownloadAvailableEmailSettings.MergeWith(customEmailSettingsModel.DownloadAvailableEmailSettings);
            }

            if (customEmailSettingsModel.DownloadFailedEmailSettings != null)
            {
                if (DownloadFailedEmailSettings == null)
                    DownloadFailedEmailSettings = customEmailSettingsModel.DownloadFailedEmailSettings;
                else
                    DownloadFailedEmailSettings.MergeWith(customEmailSettingsModel.DownloadFailedEmailSettings);
            }
            
            if (customEmailSettingsModel.DownloadDeclinedEmailSettings != null)
            {
                if (DownloadDeclinedEmailSettings == null)
                    DownloadDeclinedEmailSettings = customEmailSettingsModel.DownloadDeclinedEmailSettings;
                else
                    DownloadDeclinedEmailSettings.MergeWith(customEmailSettingsModel.DownloadDeclinedEmailSettings);
            }

            if (customEmailSettingsModel.AccessRequestDoneEmailSettings != null)
            {
                if (AccessRequestDoneEmailSettings == null)
                    AccessRequestDoneEmailSettings = customEmailSettingsModel.AccessRequestDoneEmailSettings;
                else
                    AccessRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDoneEmailSettings);
            }

            if (customEmailSettingsModel.AccessRequestDeclinedEmailSettings != null)
            {
                if (AccessRequestDeclinedEmailSettings == null)
                    AccessRequestDeclinedEmailSettings = customEmailSettingsModel.AccessRequestDeclinedEmailSettings;
                else
                    AccessRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDeclinedEmailSettings);
            }

            if (customEmailSettingsModel.PermissionRequestDoneEmailSettings != null)
            {
                if (PermissionRequestDoneEmailSettings == null)
                    PermissionRequestDoneEmailSettings = customEmailSettingsModel.PermissionRequestDoneEmailSettings;
                else
                    PermissionRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDoneEmailSettings);
            }

            if (customEmailSettingsModel.PermissionRequestDeclinedEmailSettings != null)
            {
                if (PermissionRequestDeclinedEmailSettings == null)
                    PermissionRequestDeclinedEmailSettings = customEmailSettingsModel.PermissionRequestDeclinedEmailSettings;
                else
                    PermissionRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDeclinedEmailSettings);
            }

            if (customEmailSettingsModel.EmailSenderConfiguration != null)
            {
                EmailSenderConfiguration = customEmailSettingsModel.EmailSenderConfiguration;
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
            return ResolveLocalizedText(Subject, cultureInfo);
        }

        public string GetTitle(CultureInfo cultureInfo)
        {
            return ResolveLocalizedText(Title, cultureInfo);
        }

        public string GetPreHeader(CultureInfo cultureInfo)
        {
            return ResolveLocalizedText(PreHeader, cultureInfo);
        }

        public string GetTextPrefix(CultureInfo cultureInfo)
        {
            return ResolveLocalizedText(TextPrefix, cultureInfo);
        }

        public string GetButton(CultureInfo cultureInfo)
        {
            return ResolveLocalizedText(Button, cultureInfo);
        }

        public string GetTextSuffix(CultureInfo cultureInfo)
        {
            return ResolveLocalizedText(TextSuffix, cultureInfo);
        }

        public string GetFooter(CultureInfo cultureInfo)
        {
            return ResolveLocalizedText(Footer, cultureInfo);
        }

        private string ResolveLocalizedText(Dictionary<string, string> translations, CultureInfo cultureInfo)
        {
            string culture = cultureInfo.TwoLetterISOLanguageName;

            if (translations.ContainsKey(culture) &&
                !string.IsNullOrEmpty(translations[culture]))
            {
                return translations[culture];
            }

            if (translations.ContainsKey("x-default") &&
                !string.IsNullOrEmpty(translations["x-default"]))
            {
                return translations["x-default"];
            }

            if (translations.ContainsKey("en") &&
                !string.IsNullOrEmpty(translations["en"]))
            {
                return translations["en"];
            }

            return translations.First().Value;
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
