using HCore.Identity.Database.SqlServer.Models.Impl;
using Microsoft.AspNetCore.Identity;
using Sustainsys.Saml2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace HCore.Identity.Internal
{
    // see https://github.com/Sustainsys/Saml2/blob/master/Samples/SampleAspNetCore2ApplicationNETFramework/Pages/Account/ExternalLogin.cshtml.cs

    // we had issues when the logout was not performed, because LogoutNameIdentifier and SessionIndex was not stored to
    // the user. After long search we found this code - it stores it to the user token, and then it works

    internal class Saml2SupportClaimsFactory : IUserClaimsPrincipalFactory<UserModel>
    {
        IUserClaimsPrincipalFactory<UserModel> _inner;
        ClaimsPrincipal _claimsPrincipal;

        public Saml2SupportClaimsFactory(
            IUserClaimsPrincipalFactory<UserModel> inner,
            ClaimsPrincipal claimsPrincipal)
        {
            _inner = inner;
            _claimsPrincipal = claimsPrincipal;
        }

        public async Task<ClaimsPrincipal> CreateAsync(UserModel user)
        {
            var principal = await _inner.CreateAsync(user);

            var logoutInfo = _claimsPrincipal.FindFirst(Saml2ClaimTypes.LogoutNameIdentifier);
            var sessionIndex = _claimsPrincipal.FindFirst(Saml2ClaimTypes.SessionIndex);

            var identity = principal.Identities.Single();

            if (logoutInfo != null)
                identity.AddClaim(logoutInfo);

            if (sessionIndex != null)
                identity.AddClaim(sessionIndex);

            return principal;
        }
    }
}
