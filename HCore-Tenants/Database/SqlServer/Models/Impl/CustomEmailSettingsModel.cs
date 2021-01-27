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
    }
}
