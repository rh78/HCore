using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.Models;
using System.Threading.Tasks;

namespace HCore.Identity.Services
{
    public interface IIdentityServices
    {
        Task<UserModel> CreateUserAsync(UserSpec userSpec, bool isAdmin);
        Task ConfirmUserEmailAddressAsync(string userUuid, UserConfirmEmailSpec userConfirmEmailSpec);
        Task ResendUserEmailConfirmationEmailAsync(string userUuid);

        Task UserForgotPasswordAsync(UserForgotPasswordSpec userForgotPasswordSpec);
        Task ResetUserPasswordAsync(ResetUserPasswordSpec resetUserPasswordSpec);
        Task SetUserPasswordAsync(string userUuid, SetUserPasswordSpec setUserPasswordSpec);

        Task<UserModel> SignInUserAsync(UserSignInSpec userSignInSpec);
        Task SignOutUserAsync();

        Task<UserModel> GetUserAsync(string userUuid);

        Task<UserModel> UpdateUserAsync(string userUuid, UserSpec user, bool isAdmin);

        Task<string> ReserveUserUuidAsync(string emailAddress);
    }
}
