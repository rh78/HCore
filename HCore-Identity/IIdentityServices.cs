using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.ViewModels;
using System.Threading.Tasks;

namespace HCore.Identity
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
    }
}
