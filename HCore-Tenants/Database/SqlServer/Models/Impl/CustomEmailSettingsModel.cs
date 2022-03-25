using System;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    [Serializable]
    public class CustomEmailSettingsModel
    {
        public EmailInstanceSettingsModel InvitationEmailSettings { get; set; }
        public EmailInstanceSettingsModel ConfirmAccountEmailSettings { get; set; }
        public EmailInstanceSettingsModel ForgotPasswordEmailSettings { get; set; }
        public EmailInstanceSettingsModel NewUnreadNotificationsEmailSettings { get; set; }
        public EmailInstanceSettingsModel CollectionInvitationEmailSettings { get; set; }
        public EmailInstanceSettingsModel CollectionInvitationAcceptedEmailSettings { get; set; }
        public EmailInstanceSettingsModel CollectionInvitationDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel ShareEmailSettings { get; set; }
        public EmailInstanceSettingsModel CommentCollectionEmailSettings { get; set; }
        public EmailInstanceSettingsModel CommentCollectionAssetEmailSettings { get; set; }
        public EmailInstanceSettingsModel CommentShareEmailSettings { get; set; }
        public EmailInstanceSettingsModel CommentShareAssetEmailSettings { get; set; }
        public EmailInstanceSettingsModel DownloadAvailableEmailSettings { get; set; }
        public EmailInstanceSettingsModel DownloadFailedEmailSettings { get; set; }
        public EmailInstanceSettingsModel DownloadDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel AccessRequestDoneEmailSettings { get; set; }
        public EmailInstanceSettingsModel AccessRequestDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel PermissionRequestDoneEmailSettings { get; set; }
        public EmailInstanceSettingsModel PermissionRequestDeclinedEmailSettings { get; set; }

        public SmtpEmailSenderConfigurationModel EmailSenderConfiguration { get; set; }
    }
}
