using HCore.Identity.Database.SqlServer.Models.Impl;

namespace HCore.Identity.Models
{
    public class UserNotificationModel
    {
        public string Id { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public string NotificationCulture { get; set; }

        public bool? GroupNotifications { get; set; }

        public string Currency { get; set; }

        public string ExternalUuid { get; set; }
    }
}
