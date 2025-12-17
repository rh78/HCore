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
        public EmailInstanceSettingsModel HttpsCertificateExpiresEmailSettings { get; set; }
        public EmailInstanceSettingsModel HttpsCertificateRenewEmailSettings { get; set; }
        public EmailInstanceSettingsModel HttpsCertificateRenewErrorEmailSettings { get; set; }
        public EmailInstanceSettingsModel NewAdminUserRegisteredEmailSettings { get; set; }
        public EmailInstanceSettingsModel CollectionInvitationEmailSettings { get; set; }
        public EmailInstanceSettingsModel CollectionInvitationAcceptedEmailSettings { get; set; }
        public EmailInstanceSettingsModel CollectionInvitationDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel ShareEmailSettings { get; set; }
        public EmailInstanceSettingsModel CommentCollectionEmailSettings { get; set; }
        public EmailInstanceSettingsModel CommentCollectionAssetEmailSettings { get; set; }
        public EmailInstanceSettingsModel FlagCollectionAssetEmailSettings { get; set; }
        public EmailInstanceSettingsModel CommentShareEmailSettings { get; set; }
        public EmailInstanceSettingsModel CommentShareAssetEmailSettings { get; set; }
        public EmailInstanceSettingsModel FlagShareAssetEmailSettings { get; set; }
        public EmailInstanceSettingsModel DownloadAvailableEmailSettings { get; set; }
        public EmailInstanceSettingsModel DownloadFailedEmailSettings { get; set; }
        public EmailInstanceSettingsModel DownloadDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel AccessRequestDoneEmailSettings { get; set; }
        public EmailInstanceSettingsModel AccessRequestDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel PermissionRequestDoneEmailSettings { get; set; }
        public EmailInstanceSettingsModel PermissionRequestDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel UploadAssetsDoneEmailSettings { get; set; }
        public EmailInstanceSettingsModel UploadAssetsDeclinedEmailSettings { get; set; }
        public EmailInstanceSettingsModel GenericEmailSettings { get; set; }

        public SmtpEmailSenderConfigurationModel EmailSenderConfiguration { get; set; }

        public void MergeWith(EmailSettingsModel customEmailSettingsModel, bool allowEmpty)
        {
            if (customEmailSettingsModel == null)
                return;

            if (customEmailSettingsModel.InvitationEmailSettings != null)
            {
                if (InvitationEmailSettings == null)
                {
                    InvitationEmailSettings = new EmailInstanceSettingsModel();
                    InvitationEmailSettings.MergeWith(customEmailSettingsModel.InvitationEmailSettings, allowEmpty);
                }
                else
                {
                    InvitationEmailSettings.MergeWith(customEmailSettingsModel.InvitationEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.ConfirmAccountEmailSettings != null)
            {
                if (ConfirmAccountEmailSettings == null)
                {
                    ConfirmAccountEmailSettings = new EmailInstanceSettingsModel();
                    ConfirmAccountEmailSettings.MergeWith(customEmailSettingsModel.ConfirmAccountEmailSettings, allowEmpty);
                }
                else
                {
                    ConfirmAccountEmailSettings.MergeWith(customEmailSettingsModel.ConfirmAccountEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.ForgotPasswordEmailSettings != null)
            {
                if (ForgotPasswordEmailSettings == null)
                {
                    ForgotPasswordEmailSettings = new EmailInstanceSettingsModel();
                    ForgotPasswordEmailSettings.MergeWith(customEmailSettingsModel.ForgotPasswordEmailSettings, allowEmpty);
                }
                else
                {
                    ForgotPasswordEmailSettings.MergeWith(customEmailSettingsModel.ForgotPasswordEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.NewUnreadNotificationsEmailSettings != null)
            {
                if (NewUnreadNotificationsEmailSettings == null)
                {
                    NewUnreadNotificationsEmailSettings = new EmailInstanceSettingsModel();
                    NewUnreadNotificationsEmailSettings.MergeWith(customEmailSettingsModel.NewUnreadNotificationsEmailSettings, allowEmpty);
                }
                else
                {
                    NewUnreadNotificationsEmailSettings.MergeWith(customEmailSettingsModel.NewUnreadNotificationsEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.HttpsCertificateExpiresEmailSettings != null)
            {
                if (HttpsCertificateExpiresEmailSettings == null)
                {
                    HttpsCertificateExpiresEmailSettings = new EmailInstanceSettingsModel();
                    HttpsCertificateExpiresEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateExpiresEmailSettings, allowEmpty);
                }
                else
                {
                    HttpsCertificateExpiresEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateExpiresEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.HttpsCertificateRenewEmailSettings != null)
            {
                if (HttpsCertificateRenewEmailSettings == null)
                {
                    HttpsCertificateRenewEmailSettings = new EmailInstanceSettingsModel();
                    HttpsCertificateRenewEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateRenewEmailSettings, allowEmpty);
                }
                else
                {
                    HttpsCertificateRenewEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateRenewEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.HttpsCertificateRenewErrorEmailSettings != null)
            {
                if (HttpsCertificateRenewErrorEmailSettings == null)
                {
                    HttpsCertificateRenewErrorEmailSettings = new EmailInstanceSettingsModel();
                    HttpsCertificateRenewErrorEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateRenewErrorEmailSettings, allowEmpty);
                }
                else
                {
                    HttpsCertificateRenewErrorEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateRenewErrorEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.NewAdminUserRegisteredEmailSettings != null)
            {
                if (NewAdminUserRegisteredEmailSettings == null)
                {
                    NewAdminUserRegisteredEmailSettings = new EmailInstanceSettingsModel();
                    NewAdminUserRegisteredEmailSettings.MergeWith(customEmailSettingsModel.NewAdminUserRegisteredEmailSettings, allowEmpty);
                }
                else
                {
                    NewAdminUserRegisteredEmailSettings.MergeWith(customEmailSettingsModel.NewAdminUserRegisteredEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationEmailSettings != null)
            {
                if (CollectionInvitationEmailSettings == null)
                {
                    CollectionInvitationEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationEmailSettings, allowEmpty);
                }
                else
                {
                    CollectionInvitationEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings != null)
            {
                if (CollectionInvitationAcceptedEmailSettings == null)
                {
                    CollectionInvitationAcceptedEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationAcceptedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings, allowEmpty);
                }
                else
                {
                    CollectionInvitationAcceptedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings != null)
            {
                if (CollectionInvitationDeclinedEmailSettings == null)
                {
                    CollectionInvitationDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationDeclinedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings, allowEmpty);
                }
                else
                {
                    CollectionInvitationDeclinedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.ShareEmailSettings != null)
            {
                if (ShareEmailSettings == null)
                {
                    ShareEmailSettings = new EmailInstanceSettingsModel();
                    ShareEmailSettings.MergeWith(customEmailSettingsModel.ShareEmailSettings, allowEmpty);
                }
                else
                {
                    ShareEmailSettings.MergeWith(customEmailSettingsModel.ShareEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CommentCollectionEmailSettings != null)
            {
                if (CommentCollectionEmailSettings == null)
                {
                    CommentCollectionEmailSettings = new EmailInstanceSettingsModel();
                    CommentCollectionEmailSettings.MergeWith(customEmailSettingsModel.CommentCollectionEmailSettings, allowEmpty);
                }
                else
                {
                    CommentCollectionEmailSettings.MergeWith(customEmailSettingsModel.CommentCollectionEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CommentCollectionAssetEmailSettings != null)
            {
                if (CommentCollectionAssetEmailSettings == null)
                {
                    CommentCollectionAssetEmailSettings = new EmailInstanceSettingsModel();
                    CommentCollectionAssetEmailSettings.MergeWith(customEmailSettingsModel.CommentCollectionAssetEmailSettings, allowEmpty);
                }
                else
                {
                    CommentCollectionAssetEmailSettings.MergeWith(customEmailSettingsModel.CommentCollectionAssetEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.FlagCollectionAssetEmailSettings != null)
            {
                if (FlagCollectionAssetEmailSettings == null)
                {
                    FlagCollectionAssetEmailSettings = new EmailInstanceSettingsModel();
                    FlagCollectionAssetEmailSettings.MergeWith(customEmailSettingsModel.FlagCollectionAssetEmailSettings, allowEmpty);
                }
                else
                {
                    FlagCollectionAssetEmailSettings.MergeWith(customEmailSettingsModel.FlagCollectionAssetEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CommentShareEmailSettings != null)
            {
                if (CommentShareEmailSettings == null)
                {
                    CommentShareEmailSettings = new EmailInstanceSettingsModel();
                    CommentShareEmailSettings.MergeWith(customEmailSettingsModel.CommentShareEmailSettings, allowEmpty);
                }
                else
                {
                    CommentShareEmailSettings.MergeWith(customEmailSettingsModel.CommentShareEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CommentShareAssetEmailSettings != null)
            {
                if (CommentShareAssetEmailSettings == null)
                {
                    CommentShareAssetEmailSettings = new EmailInstanceSettingsModel();
                    CommentShareAssetEmailSettings.MergeWith(customEmailSettingsModel.CommentShareAssetEmailSettings, allowEmpty);
                }
                else
                {
                    CommentShareAssetEmailSettings.MergeWith(customEmailSettingsModel.CommentShareAssetEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.FlagShareAssetEmailSettings != null)
            {
                if (FlagShareAssetEmailSettings == null)
                {
                    FlagShareAssetEmailSettings = new EmailInstanceSettingsModel();
                    FlagShareAssetEmailSettings.MergeWith(customEmailSettingsModel.FlagShareAssetEmailSettings, allowEmpty);
                }
                else
                {
                    FlagShareAssetEmailSettings.MergeWith(customEmailSettingsModel.FlagShareAssetEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.DownloadAvailableEmailSettings != null)
            {
                if (DownloadAvailableEmailSettings == null)
                {
                    DownloadAvailableEmailSettings = new EmailInstanceSettingsModel();
                    DownloadAvailableEmailSettings.MergeWith(customEmailSettingsModel.DownloadAvailableEmailSettings, allowEmpty);
                }
                else
                {
                    DownloadAvailableEmailSettings.MergeWith(customEmailSettingsModel.DownloadAvailableEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.DownloadFailedEmailSettings != null)
            {
                if (DownloadFailedEmailSettings == null)
                {
                    DownloadFailedEmailSettings = new EmailInstanceSettingsModel();
                    DownloadFailedEmailSettings.MergeWith(customEmailSettingsModel.DownloadFailedEmailSettings, allowEmpty);
                }
                else
                {
                    DownloadFailedEmailSettings.MergeWith(customEmailSettingsModel.DownloadFailedEmailSettings, allowEmpty);
                }
            }
            
            if (customEmailSettingsModel.DownloadDeclinedEmailSettings != null)
            {
                if (DownloadDeclinedEmailSettings == null)
                {
                    DownloadDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    DownloadDeclinedEmailSettings.MergeWith(customEmailSettingsModel.DownloadDeclinedEmailSettings, allowEmpty);
                }
                else
                {
                    DownloadDeclinedEmailSettings.MergeWith(customEmailSettingsModel.DownloadDeclinedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.AccessRequestDoneEmailSettings != null)
            {
                if (AccessRequestDoneEmailSettings == null)
                {
                    AccessRequestDoneEmailSettings = new EmailInstanceSettingsModel();
                    AccessRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDoneEmailSettings, allowEmpty);
                }
                else
                {
                    AccessRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDoneEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.AccessRequestDeclinedEmailSettings != null)
            {
                if (AccessRequestDeclinedEmailSettings == null)
                {
                    AccessRequestDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    AccessRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDeclinedEmailSettings, allowEmpty);
                }
                else
                {
                    AccessRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDeclinedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.PermissionRequestDoneEmailSettings != null)
            {
                if (PermissionRequestDoneEmailSettings == null)
                {
                    PermissionRequestDoneEmailSettings = new EmailInstanceSettingsModel();
                    PermissionRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDoneEmailSettings, allowEmpty);
                }
                else
                {
                    PermissionRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDoneEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.PermissionRequestDeclinedEmailSettings != null)
            {
                if (PermissionRequestDeclinedEmailSettings == null)
                {
                    PermissionRequestDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    PermissionRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDeclinedEmailSettings, allowEmpty);
                }
                else
                {
                    PermissionRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDeclinedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.UploadAssetsDoneEmailSettings != null)
            {
                if (UploadAssetsDoneEmailSettings == null)
                {
                    UploadAssetsDoneEmailSettings = new EmailInstanceSettingsModel();
                    UploadAssetsDoneEmailSettings.MergeWith(customEmailSettingsModel.UploadAssetsDoneEmailSettings, allowEmpty);
                }
                else
                {
                    UploadAssetsDoneEmailSettings.MergeWith(customEmailSettingsModel.UploadAssetsDoneEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.UploadAssetsDeclinedEmailSettings != null)
            {
                if (UploadAssetsDeclinedEmailSettings == null)
                {
                    UploadAssetsDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    UploadAssetsDeclinedEmailSettings.MergeWith(customEmailSettingsModel.UploadAssetsDeclinedEmailSettings, allowEmpty);
                }
                else
                {
                    UploadAssetsDeclinedEmailSettings.MergeWith(customEmailSettingsModel.UploadAssetsDeclinedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.GenericEmailSettings != null)
            {
                if (GenericEmailSettings == null)
                {
                    GenericEmailSettings = new EmailInstanceSettingsModel();
                    GenericEmailSettings.MergeWith(customEmailSettingsModel.GenericEmailSettings, allowEmpty);
                }
                else
                {
                    GenericEmailSettings.MergeWith(customEmailSettingsModel.GenericEmailSettings, allowEmpty);
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
                EmailSenderConfiguration.SmtpEnableExtendedLogging = customEmailSettingsModel.EmailSenderConfiguration.SmtpEnableExtendedLogging;
            }
        }

        public void MergeWith(CustomEmailSettingsModel customEmailSettingsModel, bool allowEmpty)
        {
            if (customEmailSettingsModel == null)
                return;

            if (customEmailSettingsModel.InvitationEmailSettings != null)
            {
                if (InvitationEmailSettings == null)
                {
                    InvitationEmailSettings = new EmailInstanceSettingsModel();
                    InvitationEmailSettings.MergeWith(customEmailSettingsModel.InvitationEmailSettings, allowEmpty);
                }
                else
                {
                    InvitationEmailSettings.MergeWith(customEmailSettingsModel.InvitationEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.ConfirmAccountEmailSettings != null)
            {
                if (ConfirmAccountEmailSettings == null)
                {
                    ConfirmAccountEmailSettings = new EmailInstanceSettingsModel();
                    ConfirmAccountEmailSettings.MergeWith(customEmailSettingsModel.ConfirmAccountEmailSettings, allowEmpty);
                }
                else
                {
                    ConfirmAccountEmailSettings.MergeWith(customEmailSettingsModel.ConfirmAccountEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.ForgotPasswordEmailSettings != null)
            {
                if (ForgotPasswordEmailSettings == null)
                {
                    ForgotPasswordEmailSettings = new EmailInstanceSettingsModel();
                    ForgotPasswordEmailSettings.MergeWith(customEmailSettingsModel.ForgotPasswordEmailSettings, allowEmpty);
                }
                else
                {
                    ForgotPasswordEmailSettings.MergeWith(customEmailSettingsModel.ForgotPasswordEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.NewUnreadNotificationsEmailSettings != null)
            {
                if (NewUnreadNotificationsEmailSettings == null)
                {
                    NewUnreadNotificationsEmailSettings = new EmailInstanceSettingsModel();
                    NewUnreadNotificationsEmailSettings.MergeWith(customEmailSettingsModel.NewUnreadNotificationsEmailSettings, allowEmpty);
                }
                else
                {
                    NewUnreadNotificationsEmailSettings.MergeWith(customEmailSettingsModel.NewUnreadNotificationsEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.HttpsCertificateExpiresEmailSettings != null)
            {
                if (HttpsCertificateExpiresEmailSettings == null)
                {
                    HttpsCertificateExpiresEmailSettings = new EmailInstanceSettingsModel();
                    HttpsCertificateExpiresEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateExpiresEmailSettings, allowEmpty);
                }
                else
                {
                    HttpsCertificateExpiresEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateExpiresEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.HttpsCertificateRenewEmailSettings != null)
            {
                if (HttpsCertificateRenewEmailSettings == null)
                {
                    HttpsCertificateRenewEmailSettings = new EmailInstanceSettingsModel();
                    HttpsCertificateRenewEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateRenewEmailSettings, allowEmpty);
                }
                else
                {
                    HttpsCertificateRenewEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateRenewEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.HttpsCertificateRenewErrorEmailSettings != null)
            {
                if (HttpsCertificateRenewErrorEmailSettings == null)
                {
                    HttpsCertificateRenewErrorEmailSettings = new EmailInstanceSettingsModel();
                    HttpsCertificateRenewErrorEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateRenewErrorEmailSettings, allowEmpty);
                }
                else
                {
                    HttpsCertificateRenewErrorEmailSettings.MergeWith(customEmailSettingsModel.HttpsCertificateRenewErrorEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.NewAdminUserRegisteredEmailSettings != null)
            {
                if (NewAdminUserRegisteredEmailSettings == null)
                {
                    NewAdminUserRegisteredEmailSettings = new EmailInstanceSettingsModel();
                    NewAdminUserRegisteredEmailSettings.MergeWith(customEmailSettingsModel.NewAdminUserRegisteredEmailSettings, allowEmpty);
                }
                else
                {
                    NewAdminUserRegisteredEmailSettings.MergeWith(customEmailSettingsModel.NewAdminUserRegisteredEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationEmailSettings != null)
            {
                if (CollectionInvitationEmailSettings == null)
                {
                    CollectionInvitationEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationEmailSettings, allowEmpty);
                }
                else
                {
                    CollectionInvitationEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings != null)
            {
                if (CollectionInvitationAcceptedEmailSettings == null)
                {
                    CollectionInvitationAcceptedEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationAcceptedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings, allowEmpty);
                }
                else
                {
                    CollectionInvitationAcceptedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationAcceptedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings != null)
            {
                if (CollectionInvitationDeclinedEmailSettings == null)
                {
                    CollectionInvitationDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    CollectionInvitationDeclinedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings, allowEmpty);
                }
                else
                {
                    CollectionInvitationDeclinedEmailSettings.MergeWith(customEmailSettingsModel.CollectionInvitationDeclinedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.ShareEmailSettings != null)
            {
                if (ShareEmailSettings == null)
                {
                    ShareEmailSettings = new EmailInstanceSettingsModel();
                    ShareEmailSettings.MergeWith(customEmailSettingsModel.ShareEmailSettings, allowEmpty);
                }
                else
                {
                    ShareEmailSettings.MergeWith(customEmailSettingsModel.ShareEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CommentCollectionEmailSettings != null)
            {
                if (CommentCollectionEmailSettings == null)
                {
                    CommentCollectionEmailSettings = new EmailInstanceSettingsModel();
                    CommentCollectionEmailSettings.MergeWith(customEmailSettingsModel.CommentCollectionEmailSettings, allowEmpty);
                }
                else
                {
                    CommentCollectionEmailSettings.MergeWith(customEmailSettingsModel.CommentCollectionEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CommentCollectionAssetEmailSettings != null)
            {
                if (CommentCollectionAssetEmailSettings == null)
                {
                    CommentCollectionAssetEmailSettings = new EmailInstanceSettingsModel();
                    CommentCollectionAssetEmailSettings.MergeWith(customEmailSettingsModel.CommentCollectionAssetEmailSettings, allowEmpty);
                }
                else
                {
                    CommentCollectionAssetEmailSettings.MergeWith(customEmailSettingsModel.CommentCollectionAssetEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.FlagCollectionAssetEmailSettings != null)
            {
                if (FlagCollectionAssetEmailSettings == null)
                {
                    FlagCollectionAssetEmailSettings = new EmailInstanceSettingsModel();
                    FlagCollectionAssetEmailSettings.MergeWith(customEmailSettingsModel.FlagCollectionAssetEmailSettings, allowEmpty);
                }
                else
                {
                    FlagCollectionAssetEmailSettings.MergeWith(customEmailSettingsModel.FlagCollectionAssetEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CommentShareEmailSettings != null)
            {
                if (CommentShareEmailSettings == null)
                {
                    CommentShareEmailSettings = new EmailInstanceSettingsModel();
                    CommentShareEmailSettings.MergeWith(customEmailSettingsModel.CommentShareEmailSettings, allowEmpty);
                }
                else
                {
                    CommentShareEmailSettings.MergeWith(customEmailSettingsModel.CommentShareEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.CommentShareAssetEmailSettings != null)
            {
                if (CommentShareAssetEmailSettings == null)
                {
                    CommentShareAssetEmailSettings = new EmailInstanceSettingsModel();
                    CommentShareAssetEmailSettings.MergeWith(customEmailSettingsModel.CommentShareAssetEmailSettings, allowEmpty);
                }
                else
                {
                    CommentShareAssetEmailSettings.MergeWith(customEmailSettingsModel.CommentShareAssetEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.FlagShareAssetEmailSettings != null)
            {
                if (FlagShareAssetEmailSettings == null)
                {
                    FlagShareAssetEmailSettings = new EmailInstanceSettingsModel();
                    FlagShareAssetEmailSettings.MergeWith(customEmailSettingsModel.FlagShareAssetEmailSettings, allowEmpty);
                }
                else
                {
                    FlagShareAssetEmailSettings.MergeWith(customEmailSettingsModel.FlagShareAssetEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.DownloadAvailableEmailSettings != null)
            {
                if (DownloadAvailableEmailSettings == null)
                {
                    DownloadAvailableEmailSettings = new EmailInstanceSettingsModel();
                    DownloadAvailableEmailSettings.MergeWith(customEmailSettingsModel.DownloadAvailableEmailSettings, allowEmpty);
                }
                else
                {
                    DownloadAvailableEmailSettings.MergeWith(customEmailSettingsModel.DownloadAvailableEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.DownloadFailedEmailSettings != null)
            {
                if (DownloadFailedEmailSettings == null)
                {
                    DownloadFailedEmailSettings = new EmailInstanceSettingsModel();
                    DownloadFailedEmailSettings.MergeWith(customEmailSettingsModel.DownloadFailedEmailSettings, allowEmpty);
                }
                else
                {
                    DownloadFailedEmailSettings.MergeWith(customEmailSettingsModel.DownloadFailedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.DownloadDeclinedEmailSettings != null)
            {
                if (DownloadDeclinedEmailSettings == null)
                {
                    DownloadDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    DownloadDeclinedEmailSettings.MergeWith(customEmailSettingsModel.DownloadDeclinedEmailSettings, allowEmpty);
                }
                else
                {
                    DownloadDeclinedEmailSettings.MergeWith(customEmailSettingsModel.DownloadDeclinedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.AccessRequestDoneEmailSettings != null)
            {
                if (AccessRequestDoneEmailSettings == null)
                {
                    AccessRequestDoneEmailSettings = new EmailInstanceSettingsModel();
                    AccessRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDoneEmailSettings, allowEmpty);
                }
                else
                {
                    AccessRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDoneEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.AccessRequestDeclinedEmailSettings != null)
            {
                if (AccessRequestDeclinedEmailSettings == null)
                {
                    AccessRequestDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    AccessRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDeclinedEmailSettings, allowEmpty);
                }
                else
                {
                    AccessRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.AccessRequestDeclinedEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.PermissionRequestDoneEmailSettings != null)
            {
                if (PermissionRequestDoneEmailSettings == null)
                {
                    PermissionRequestDoneEmailSettings = new EmailInstanceSettingsModel();
                    PermissionRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDoneEmailSettings, allowEmpty);
                }
                else
                {
                    PermissionRequestDoneEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDoneEmailSettings, allowEmpty);
                }
            }

            if (customEmailSettingsModel.PermissionRequestDeclinedEmailSettings != null)
            {
                if (PermissionRequestDeclinedEmailSettings == null)
                {
                    PermissionRequestDeclinedEmailSettings = new EmailInstanceSettingsModel();
                    PermissionRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDeclinedEmailSettings, allowEmpty);
                }
                else
                {
                    PermissionRequestDeclinedEmailSettings.MergeWith(customEmailSettingsModel.PermissionRequestDeclinedEmailSettings, allowEmpty);
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
                EmailSenderConfiguration.SmtpEnableExtendedLogging = customEmailSettingsModel.EmailSenderConfiguration.SmtpEnableExtendedLogging;
            }
        }

        public void Validate(bool allowEmpty)
        {
            if (InvitationEmailSettings == null)
                throw new Exception("Invitation email settings are missing");

            InvitationEmailSettings.Validate(allowEmpty);

            if (ConfirmAccountEmailSettings == null)
                throw new Exception("Confirm account email settings are missing");

            ConfirmAccountEmailSettings.Validate(allowEmpty);

            if (ForgotPasswordEmailSettings == null)
                throw new Exception("Forgot password email settings are missing");

            ForgotPasswordEmailSettings.Validate(allowEmpty);

            if (NewUnreadNotificationsEmailSettings == null)
                throw new Exception("New unread notifications email settings are missing");

            NewUnreadNotificationsEmailSettings.Validate(allowEmpty);
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

        public void Validate(bool allowEmpty)
        {
            if (Subject == null || Subject.Count == 0)
                throw new Exception("Subject is missing");

            if (Title == null || Title.Count == 0)
                throw new Exception("Title is missing");

            if (!allowEmpty)
            {
                if (PreHeader == null || PreHeader.Count == 0)
                    throw new Exception("Preheader is missing");
            }

            if (Button == null || Button.Count == 0)
                throw new Exception("Button is missing");

            if (Footer == null || Footer.Count == 0)
                throw new Exception("Footer is missing");
        }

        public void MergeWith(EmailInstanceSettingsModel emailInstanceSettingsModel, bool allowEmpty)
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
            else if (allowEmpty)
            {
                PreHeader = null;
            }

            if (emailInstanceSettingsModel.TextPrefix != null &&
                emailInstanceSettingsModel.TextPrefix.Count > 0)
            {
                TextPrefix = emailInstanceSettingsModel.TextPrefix;
            }
            else if (allowEmpty)
            {
                TextPrefix = null;
            }

            if (emailInstanceSettingsModel.TextSuffix != null &&
                emailInstanceSettingsModel.TextSuffix.Count > 0)
            {
                TextSuffix = emailInstanceSettingsModel.TextSuffix;
            }
            else if (allowEmpty)
            {
                TextSuffix = null;
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
