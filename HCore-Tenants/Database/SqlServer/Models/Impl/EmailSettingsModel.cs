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
                {
                    InvitationEmailSettings = new EmailInstanceSettingsModel();
                    InvitationEmailSettings.MergeWith(customEmailSettingsModel.InvitationEmailSettings);
                }
                else
                {
                    InvitationEmailSettings.MergeWith(customEmailSettingsModel.InvitationEmailSettings);
                }
            }

            if (customEmailSettingsModel.ConfirmAccountEmailSettings != null)
            {
                if (ConfirmAccountEmailSettings == null)
                {
                    ConfirmAccountEmailSettings = new EmailInstanceSettingsModel();
                    ConfirmAccountEmailSettings.MergeWith(customEmailSettingsModel.ConfirmAccountEmailSettings);
                }
                else
                {
                    ConfirmAccountEmailSettings.MergeWith(customEmailSettingsModel.ConfirmAccountEmailSettings);
                }
            }

            if (customEmailSettingsModel.ForgotPasswordEmailSettings != null)
            {
                if (ForgotPasswordEmailSettings == null)
                {
                    ForgotPasswordEmailSettings = new EmailInstanceSettingsModel();
                    ForgotPasswordEmailSettings.MergeWith(customEmailSettingsModel.ForgotPasswordEmailSettings);
                }
                else
                {
                    ForgotPasswordEmailSettings.MergeWith(customEmailSettingsModel.ForgotPasswordEmailSettings);
                }
            }

            if (customEmailSettingsModel.NewUnreadNotificationsEmailSettings != null)
            {
                if (NewUnreadNotificationsEmailSettings == null)
                {
                    NewUnreadNotificationsEmailSettings = new EmailInstanceSettingsModel();
                    NewUnreadNotificationsEmailSettings.MergeWith(customEmailSettingsModel.NewUnreadNotificationsEmailSettings);
                }
                else
                {
                    NewUnreadNotificationsEmailSettings.MergeWith(customEmailSettingsModel.NewUnreadNotificationsEmailSettings);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationEmailSettings != null)
            {
                if (CollectionInvitationEmailSettings == null)
                {
                    CollectionInvitationEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationEmailSettings);
                }
                else
                {
                    CollectionInvitationEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationEmailSettings);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings != null)
            {
                if (CollectionInvitationAcceptedEmailSettings == null)
                {
                    CollectionInvitationAcceptedEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationAcceptedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings);
                }
                else
                {
                    CollectionInvitationAcceptedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings != null)
            {
                if (CollectionInvitationDeclinedEmailSettings == null)
                {
                    CollectionInvitationDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationDeclinedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings);
                }
                else
                {
                    CollectionInvitationDeclinedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings);
                }
            }

            if (customEmailSettingsModel.DownloadAvailableEmailSettings != null)
            {
                if (DownloadAvailableEmailSettings == null)
                {
                    DownloadAvailableEmailSettings = new EmailInstanceSettingsModel();
                    DownloadAvailableEmailSettings.MergeWith(customEmailSettingsModel.DownloadAvailableEmailSettings);
                }
                else
                {
                    DownloadAvailableEmailSettings.MergeWith(customEmailSettingsModel.DownloadAvailableEmailSettings);
                }
            }

            if (customEmailSettingsModel.DownloadFailedEmailSettings != null)
            {
                if (DownloadFailedEmailSettings == null)
                {
                    DownloadFailedEmailSettings = new EmailInstanceSettingsModel();
                    DownloadFailedEmailSettings.MergeWith(customEmailSettingsModel.DownloadFailedEmailSettings);
                }
                else
                {
                    DownloadFailedEmailSettings.MergeWith(customEmailSettingsModel.DownloadFailedEmailSettings);
                }
            }
            
            if (customEmailSettingsModel.DownloadDeclinedEmailSettings != null)
            {
                if (DownloadDeclinedEmailSettings == null)
                {
                    DownloadDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    DownloadDeclinedEmailSettings.MergeWith(customEmailSettingsModel.DownloadDeclinedEmailSettings);
                }
                else
                {
                    DownloadDeclinedEmailSettings.MergeWith(customEmailSettingsModel.DownloadDeclinedEmailSettings);
                }
            }

            if (customEmailSettingsModel.AccessRequestDoneEmailSettings != null)
            {
                if (AccessRequestDoneEmailSettings == null)
                {
                    AccessRequestDoneEmailSettings = new EmailInstanceSettingsModel();
                    AccessRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDoneEmailSettings);
                }
                else
                {
                    AccessRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDoneEmailSettings);
                }
            }

            if (customEmailSettingsModel.AccessRequestDeclinedEmailSettings != null)
            {
                if (AccessRequestDeclinedEmailSettings == null)
                {
                    AccessRequestDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    AccessRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDeclinedEmailSettings);
                }
                else
                {
                    AccessRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDeclinedEmailSettings);
                }
            }

            if (customEmailSettingsModel.PermissionRequestDoneEmailSettings != null)
            {
                if (PermissionRequestDoneEmailSettings == null)
                {
                    PermissionRequestDoneEmailSettings = new EmailInstanceSettingsModel();
                    PermissionRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDoneEmailSettings);
                }
                else
                {
                    PermissionRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDoneEmailSettings);
                }
            }

            if (customEmailSettingsModel.PermissionRequestDeclinedEmailSettings != null)
            {
                if (PermissionRequestDeclinedEmailSettings == null)
                {
                    PermissionRequestDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    PermissionRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDeclinedEmailSettings);
                }
                else
                {
                    PermissionRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDeclinedEmailSettings);
                }
            }

            if (customEmailSettingsModel.EmailSenderConfiguration != null)
            {
                EmailSenderConfiguration = new SmtpEmailSenderConfigurationModel();

                EmailSenderConfiguration.SmtpEmailAddress = customEmailSettingsModel.EmailSenderConfiguration.SmtpEmailAddress;
                EmailSenderConfiguration.SmtpFromDisplayName = customEmailSettingsModel.EmailSenderConfiguration.SmtpFromDisplayName;
                EmailSenderConfiguration.SmtpHost = customEmailSettingsModel.EmailSenderConfiguration.SmtpHost;
                EmailSenderConfiguration.SmtpPort = customEmailSettingsModel.EmailSenderConfiguration.SmtpPort;
                EmailSenderConfiguration.SmtpUserName = customEmailSettingsModel.EmailSenderConfiguration.SmtpUserName;
                EmailSenderConfiguration.SmtpPassword = customEmailSettingsModel.EmailSenderConfiguration.SmtpPassword;
                EmailSenderConfiguration.SmtpEnableSsl = customEmailSettingsModel.EmailSenderConfiguration.SmtpEnableSsl;
                EmailSenderConfiguration.SmtpStartTls = customEmailSettingsModel.EmailSenderConfiguration.SmtpStartTls;
            }
        }

        public void MergeWith(CustomEmailSettingsModel customEmailSettingsModel)
        {
            if (customEmailSettingsModel == null)
                return;

            if (customEmailSettingsModel.InvitationEmailSettings != null)
            {
                if (InvitationEmailSettings == null)
                {
                    InvitationEmailSettings = new EmailInstanceSettingsModel();
                    InvitationEmailSettings.MergeWith(customEmailSettingsModel.InvitationEmailSettings);
                }
                else
                {
                    InvitationEmailSettings.MergeWith(customEmailSettingsModel.InvitationEmailSettings);
                }
            }

            if (customEmailSettingsModel.ConfirmAccountEmailSettings != null)
            {
                if (ConfirmAccountEmailSettings == null)
                {
                    ConfirmAccountEmailSettings = new EmailInstanceSettingsModel();
                    ConfirmAccountEmailSettings.MergeWith(customEmailSettingsModel.ConfirmAccountEmailSettings);
                }
                else
                {
                    ConfirmAccountEmailSettings.MergeWith(customEmailSettingsModel.ConfirmAccountEmailSettings);
                }
            }

            if (customEmailSettingsModel.ForgotPasswordEmailSettings != null)
            {
                if (ForgotPasswordEmailSettings == null)
                {
                    ForgotPasswordEmailSettings = new EmailInstanceSettingsModel();
                    ForgotPasswordEmailSettings.MergeWith(customEmailSettingsModel.ForgotPasswordEmailSettings);
                }
                else
                {
                    ForgotPasswordEmailSettings.MergeWith(customEmailSettingsModel.ForgotPasswordEmailSettings);
                }
            }

            if (customEmailSettingsModel.NewUnreadNotificationsEmailSettings != null)
            {
                if (NewUnreadNotificationsEmailSettings == null)
                {
                    NewUnreadNotificationsEmailSettings = new EmailInstanceSettingsModel();
                    NewUnreadNotificationsEmailSettings.MergeWith(customEmailSettingsModel.NewUnreadNotificationsEmailSettings);
                }
                else
                {
                    NewUnreadNotificationsEmailSettings.MergeWith(customEmailSettingsModel.NewUnreadNotificationsEmailSettings);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationEmailSettings != null)
            {
                if (CollectionInvitationEmailSettings == null)
                {
                    CollectionInvitationEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationEmailSettings);
                }
                else
                {
                    CollectionInvitationEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationEmailSettings);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings != null)
            {
                if (CollectionInvitationAcceptedEmailSettings == null)
                {
                    CollectionInvitationAcceptedEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationAcceptedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings);
                }
                else
                {
                    CollectionInvitationAcceptedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings != null)
            {
                if (CollectionInvitationDeclinedEmailSettings == null)
                {
                    CollectionInvitationDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationDeclinedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings);
                }
                else
                {
                    CollectionInvitationDeclinedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings);
                }
            }

            if (customEmailSettingsModel.DownloadAvailableEmailSettings != null)
            {
                if (DownloadAvailableEmailSettings == null)
                {
                    DownloadAvailableEmailSettings = new EmailInstanceSettingsModel();
                    DownloadAvailableEmailSettings.MergeWith(customEmailSettingsModel.DownloadAvailableEmailSettings);
                }
                else
                {
                    DownloadAvailableEmailSettings.MergeWith(customEmailSettingsModel.DownloadAvailableEmailSettings);
                }
            }

            if (customEmailSettingsModel.DownloadFailedEmailSettings != null)
            {
                if (DownloadFailedEmailSettings == null)
                {
                    DownloadFailedEmailSettings = new EmailInstanceSettingsModel();
                    DownloadFailedEmailSettings.MergeWith(customEmailSettingsModel.DownloadFailedEmailSettings);
                }
                else
                {
                    DownloadFailedEmailSettings.MergeWith(customEmailSettingsModel.DownloadFailedEmailSettings);
                }
            }

            if (customEmailSettingsModel.DownloadDeclinedEmailSettings != null)
            {
                if (DownloadDeclinedEmailSettings == null)
                {
                    DownloadDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    DownloadDeclinedEmailSettings.MergeWith(customEmailSettingsModel.DownloadDeclinedEmailSettings);
                }
                else
                {
                    DownloadDeclinedEmailSettings.MergeWith(customEmailSettingsModel.DownloadDeclinedEmailSettings);
                }
            }

            if (customEmailSettingsModel.AccessRequestDoneEmailSettings != null)
            {
                if (AccessRequestDoneEmailSettings == null)
                {
                    AccessRequestDoneEmailSettings = new EmailInstanceSettingsModel();
                    AccessRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDoneEmailSettings);
                }
                else
                {
                    AccessRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDoneEmailSettings);
                }
            }

            if (customEmailSettingsModel.AccessRequestDeclinedEmailSettings != null)
            {
                if (AccessRequestDeclinedEmailSettings == null)
                {
                    AccessRequestDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    AccessRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDeclinedEmailSettings);
                }
                else
                {
                    AccessRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDeclinedEmailSettings);
                }
            }

            if (customEmailSettingsModel.PermissionRequestDoneEmailSettings != null)
            {
                if (PermissionRequestDoneEmailSettings == null)
                {
                    PermissionRequestDoneEmailSettings = new EmailInstanceSettingsModel();
                    PermissionRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDoneEmailSettings);
                }
                else
                {
                    PermissionRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDoneEmailSettings);
                }
            }

            if (customEmailSettingsModel.PermissionRequestDeclinedEmailSettings != null)
            {
                if (PermissionRequestDeclinedEmailSettings == null)
                {
                    PermissionRequestDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    PermissionRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDeclinedEmailSettings);
                }
                else
                {
                    PermissionRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDeclinedEmailSettings);
                }
            }

            if (customEmailSettingsModel.EmailSenderConfiguration != null)
            {
                EmailSenderConfiguration = new SmtpEmailSenderConfigurationModel();

                EmailSenderConfiguration.SmtpEmailAddress = customEmailSettingsModel.EmailSenderConfiguration.SmtpEmailAddress;
                EmailSenderConfiguration.SmtpFromDisplayName = customEmailSettingsModel.EmailSenderConfiguration.SmtpFromDisplayName;
                EmailSenderConfiguration.SmtpHost = customEmailSettingsModel.EmailSenderConfiguration.SmtpHost;
                EmailSenderConfiguration.SmtpPort = customEmailSettingsModel.EmailSenderConfiguration.SmtpPort;
                EmailSenderConfiguration.SmtpUserName = customEmailSettingsModel.EmailSenderConfiguration.SmtpUserName;
                EmailSenderConfiguration.SmtpPassword = customEmailSettingsModel.EmailSenderConfiguration.SmtpPassword;
                EmailSenderConfiguration.SmtpEnableSsl = customEmailSettingsModel.EmailSenderConfiguration.SmtpEnableSsl;
                EmailSenderConfiguration.SmtpStartTls = customEmailSettingsModel.EmailSenderConfiguration.SmtpStartTls;
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

            if (Button == null || Button.Count == 0)
                throw new Exception("Button is missing");

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
