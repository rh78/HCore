using System.Threading.Tasks;
using HCore.Identity.Models;

namespace HCore.Identity.Listeners
{
    public interface IUserNotificationListener
    {
        Task UserReservedUuidAsync(string userUuid, string emailAddress);

        Task UserCreatedAsync(UserNotificationModel userNotificationModel);
        Task UserUpdatedAsync(string userUuid, UserNotificationModel oldUserNotificationModel, UserNotificationModel newUserNotificationModel);

        Task UserLoggedInAsync(string userUuid);
        Task UserLoggedOutAsync(string userUuid);

        Task UserForgotPasswordAsync(string userUuid);
        Task UserResetPasswordAsync(string userUuid);
        Task UserSetPasswordAsync(string userUuid);

        Task UserConfirmedEmailAsync(string userUuid);
        Task UserResentEmailConfirmationEmailAsync(string userUuid);
    }
}
