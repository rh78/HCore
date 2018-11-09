using HCore.Identity.Models;

namespace HCore.Identity.Services
{
    public interface IAuthServices
    {
        IAuthInfo AuthInfo { get; }
    }
}
