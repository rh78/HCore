using System.Threading.Tasks;
using HCore.Identity.Database.SqlServer.Models.Impl;

namespace HCore.Identity.Providers
{
    public interface IUserNotificationProvider
    {
        Task UserReserveUuidAsync(string userUuid, string emailAddress);

        Task UserCreatedAsync(UserModel user);
        Task UserUpdateAsync(string userUuid, UserModel oldUser, UserModel newUser);

        Task UserLoggedInAsync(string userUuid);
        Task UserLoggedOutAsync(string userUuid);

        Task UserForgotPasswordAsync(string userUuid);
        Task UserResetPasswordAsync(string userUuid);
        Task UserSetPasswordAsync(string userUuid);

        Task UserConfirmedEmailAsync(string userUuid);
        Task UserResendEmailConfirmationEmailAsync(string userUuid);
    }
}
