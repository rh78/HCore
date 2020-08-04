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
    }
}
