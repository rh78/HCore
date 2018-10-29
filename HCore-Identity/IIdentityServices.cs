using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.ViewModels;
using System.Threading.Tasks;

namespace HCore.Identity
{
    public interface IIdentityServices
    {
        Task ConfirmUserEmailAddressAsync(string userUuid, UserConfirmEmailSpec userConfirmEmailSpec);
        Task<UserModel> CreateUserAsync(UserSpec userSpec);
        Task<UserModel> GetUserAsync(string userUuid);
        Task ResendUserEmailConfirmationEmailAsync(string userUuid);
        Task ResetUserPasswordAsync(ResetUserPasswordSpec resetUserPasswordSpec);
        Task SetUserPasswordAsync(string userUuid, SetUserPasswordSpec setUserPasswordSpec);
        Task<UserModel> SignInUserAsync(UserSignInSpec userSignInSpec);
        Task SignOutUserAsync();
        Task<UserModel> UpdateUserAsync(string userUuid, UserSpec user);
        Task UserForgotPasswordAsync(UserForgotPasswordSpec userForgotPasswordSpec);
    }
}
